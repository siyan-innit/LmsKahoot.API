using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LmsKahoot.API.Dtos;

namespace LmsKahoot.API.Services
{
    /// <summary>
    /// In-memory manager for live quiz sessions.
    /// This is the "brain" used by SignalR hubs and controllers.
    /// </summary>
    public class SessionManager
    {
        // Simple singleton for now (good enough for this project)
        private static readonly Lazy<SessionManager> _instance =
            new Lazy<SessionManager>(() => new SessionManager());

        public static SessionManager Instance => _instance.Value;

        // In-memory sessions: SessionId -> runtime state
        private readonly ConcurrentDictionary<int, SessionRuntimeState> _sessions =
            new ConcurrentDictionary<int, SessionRuntimeState>();

        private SessionManager()
        {
        }

        #region Runtime state classes (internal only)

        private class SessionRuntimeState
        {
            public int SessionId { get; set; }

            // Lobby / InProgress / BetweenQuestions / Completed
            public SessionStatus Status { get; set; } = SessionStatus.Lobby;

            // -1 means quiz not started yet
            public int CurrentQuestionIndex { get; set; } = -1;

            public int? CurrentQuestionId { get; set; }

            public DateTime? QuestionStartUtc { get; set; }

            // For now one global time-limit; overridden when question starts
            public int TimeLimitSeconds { get; set; } = 30;

            // participantId -> runtime info
            public Dictionary<int, ParticipantRuntimeState> Participants { get; set; } =
                new Dictionary<int, ParticipantRuntimeState>();
        }

        private class ParticipantRuntimeState
        {
            public int ParticipantId { get; set; }
            public string DisplayName { get; set; }
            public int TotalScore { get; set; }
            public int? AverageResponseTimeMs { get; set; }

            // For more advanced logic we could also track per-question, but DB does that.
        }

        #endregion

        /// <summary>
        /// Initialize a new session in memory when a QuizSession row
        /// is created in the database.
        /// </summary>
        public SessionStateDto InitializeSession(int sessionId, int timeLimitSecondsPerQuestion = 30)
        {
            var state = new SessionRuntimeState
            {
                SessionId = sessionId,
                Status = SessionStatus.Lobby,
                CurrentQuestionIndex = -1,
                CurrentQuestionId = null,
                QuestionStartUtc = null,
                TimeLimitSeconds = timeLimitSecondsPerQuestion > 0 ? timeLimitSecondsPerQuestion : 30,
                Participants = new Dictionary<int, ParticipantRuntimeState>()
            };

            _sessions[sessionId] = state;

            return GetSessionState(sessionId);
        }

        /// <summary>
        /// Add a participant to an existing session (for join / late join).
        /// Returns the created ParticipantDto and the updated SessionState.
        /// </summary>
        public (ParticipantDto participant, SessionStateDto state) AddParticipant(
            int sessionId,
            int participantId,
            string displayName)
        {
            if (!_sessions.TryGetValue(sessionId, out var state))
            {
                // If session wasn't in memory (e.g. app restarted), create a basic one
                state = new SessionRuntimeState
                {
                    SessionId = sessionId,
                    Status = SessionStatus.Lobby,
                    CurrentQuestionIndex = -1,
                    CurrentQuestionId = null,
                    QuestionStartUtc = null,
                    TimeLimitSeconds = 30,
                    Participants = new Dictionary<int, ParticipantRuntimeState>()
                };
                _sessions[sessionId] = state;
            }

            if (!state.Participants.ContainsKey(participantId))
            {
                state.Participants[participantId] = new ParticipantRuntimeState
                {
                    ParticipantId = participantId,
                    DisplayName = displayName,
                    TotalScore = 0,
                    AverageResponseTimeMs = null
                };
            }

            var participantState = state.Participants[participantId];

            var participantDto = new ParticipantDto
            {
                ParticipantId = participantState.ParticipantId,
                DisplayName = participantState.DisplayName,
                TotalScore = participantState.TotalScore,
                AverageResponseTimeMs = participantState.AverageResponseTimeMs,
                IsConnected = true
            };

            var snapshot = GetSessionState(sessionId);
            return (participantDto, snapshot);
        }

        /// <summary>
        /// Start a question for a given session.
        /// This sets the current question index/id, time limit, and start time.
        /// </summary>
        public SessionStateDto StartQuestion(int sessionId, int questionId, int questionIndex, int timeLimitSeconds)
        {
            if (!_sessions.TryGetValue(sessionId, out var state))
            {
                // If session is not in memory yet, initialize a basic one
                state = new SessionRuntimeState
                {
                    SessionId = sessionId,
                    Status = SessionStatus.Lobby,
                    CurrentQuestionIndex = -1,
                    CurrentQuestionId = null,
                    QuestionStartUtc = null,
                    TimeLimitSeconds = 30,
                    Participants = new Dictionary<int, ParticipantRuntimeState>()
                };
                _sessions[sessionId] = state;
            }

            state.Status = SessionStatus.InProgress;
            state.CurrentQuestionIndex = questionIndex;
            state.CurrentQuestionId = questionId;
            state.TimeLimitSeconds = timeLimitSeconds > 0 ? timeLimitSeconds : 30;
            state.QuestionStartUtc = DateTime.UtcNow;

            return GetSessionState(sessionId);
        }

        /// <summary>
        /// Marks the current question as ended (no more answers accepted).
        /// </summary>
        public SessionStateDto EndQuestion(int sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var state))
            {
                return null;
            }

            state.Status = SessionStatus.BetweenQuestions;
            // We keep CurrentQuestionId and QuestionStartUtc for history if needed

            return GetSessionState(sessionId);
        }

        /// <summary>
        /// Apply scoring for an answer and update leaderboard.
        /// Called AFTER the hub has validated and stored the answer in DB.
        /// </summary>
        public SessionStateDto ApplyAnswerScore(int sessionId, int participantId, int responseTimeMs, int scoreEarned)
        {
            if (!_sessions.TryGetValue(sessionId, out var state))
            {
                return null;
            }

            if (!state.Participants.TryGetValue(participantId, out var participant))
            {
                return null;
            }

            // Update total score
            participant.TotalScore += scoreEarned;

            // Update average response time (simple smoothed average)
            if (responseTimeMs > 0)
            {
                if (participant.AverageResponseTimeMs == null)
                {
                    participant.AverageResponseTimeMs = responseTimeMs;
                }
                else
                {
                    participant.AverageResponseTimeMs =
                        (participant.AverageResponseTimeMs.Value + responseTimeMs) / 2;
                }
            }

            return GetSessionState(sessionId);
        }

        /// <summary>
        /// Returns a snapshot of the current session state for late join / sync.
        /// Returns null if session not tracked in memory.
        /// </summary>
        public SessionStateDto GetSessionState(int sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var state))
            {
                return null;
            }

            var participants = state.Participants.Values
                .Select(p => new ParticipantDto
                {
                    ParticipantId = p.ParticipantId,
                    DisplayName = p.DisplayName,
                    TotalScore = p.TotalScore,
                    AverageResponseTimeMs = p.AverageResponseTimeMs,
                    IsConnected = true
                })
                .ToList();

            var leaderboard = participants
                .OrderByDescending(p => p.TotalScore)
                .ThenBy(p => p.AverageResponseTimeMs ?? int.MaxValue)
                .Select((p, index) => new LeaderboardEntryDto
                {
                    ParticipantId = p.ParticipantId,
                    DisplayName = p.DisplayName,
                    TotalScore = p.TotalScore,
                    Rank = index + 1,
                    AverageResponseTimeMs = p.AverageResponseTimeMs
                })
                .ToList();

            return new SessionStateDto
            {
                SessionId = state.SessionId,
                Status = state.Status,
                CurrentQuestionIndex = state.CurrentQuestionIndex,
                CurrentQuestionId = state.CurrentQuestionId,
                QuestionStartUtc = state.QuestionStartUtc,
                TimeLimitSeconds = state.TimeLimitSeconds,
                Participants = participants,
                Leaderboard = leaderboard,
                ServerUtcNow = DateTime.UtcNow
            };
        }

        // Later we’ll add:
        // - EndSession(...)
    }
}
