using System;
using System.Linq;
using System.Threading.Tasks;
using LmsKahoot.API.Data;
using LmsKahoot.API.Dtos;
using LmsKahoot.API.Models;
using LmsKahoot.API.Services;
using Microsoft.AspNet.SignalR;
using System.Collections.Generic;
namespace LmsKahoot.API.Hubs
{
    /// <summary>
    /// SignalR hub for real-time quiz sessions.
    /// Frontend connects here for join, answers, and state sync.
    /// </summary>
    public class QuizHub : Hub
    {
        private readonly LmsKahootContext _context;

        public QuizHub()
            : this(new LmsKahootContext())
        {
        }

        // For testing / DI if ever needed
        public QuizHub(LmsKahootContext context)
        {
            _context = context;
        }

        /// <summary>
        /// CLIENT -> SERVER
        /// Student joins a quiz session using a session code (PIN) and display name.
        /// Frontend will call:
        ///   hubConnection.invoke("JoinSession", sessionCode, displayName)
        /// </summary>
        public async Task JoinSession(string sessionCode, string displayName)
        {
            if (string.IsNullOrWhiteSpace(sessionCode))
            {
                Clients.Caller.JoinFailed("Session code is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "Anonymous";
            }

            sessionCode = sessionCode.Trim();

            // 1) Find the session in DB by code
            var session = _context.QuizSessions
                .SingleOrDefault(s => s.SessionCode == sessionCode);

            if (session == null)
            {
                Clients.Caller.JoinFailed("Session not found.");
                return;
            }

            if (string.Equals(session.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                Clients.Caller.JoinFailed("Session has already ended.");
                return;
            }

            // 2) Create participant row in DB
            var participant = new QuizSessionParticipant
            {
                SessionId = session.SessionId,
                DisplayName = displayName,
                JoinedAt = DateTime.UtcNow
            };

            _context.QuizSessionParticipants.Add(participant);
            _context.SaveChanges(); // now ParticipantId is set

            // 3) Register participant in in-memory session state
            var (participantDto, state) = SessionManager.Instance.AddParticipant(
                session.SessionId,
                participant.ParticipantId,
                displayName);

            var groupName = GetGroupName(session.SessionId);

            // 4) Add this SignalR connection to the session group
            await Groups.Add(Context.ConnectionId, groupName);

            // 5) Send full session snapshot to the NEWLY JOINED client (late-join sync)
            //    Also include their own participant info
            Clients.Caller.SessionState(state, participantDto);

            // 6) Notify all OTHER clients in this session that a new participant joined
            Clients.OthersInGroup(groupName).ParticipantJoined(participantDto);
        }

        private static string GetGroupName(int sessionId)
        {
            return $"session-{sessionId}";
        }
    }
}
