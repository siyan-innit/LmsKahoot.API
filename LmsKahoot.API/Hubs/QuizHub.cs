using System;
using System.Linq;
using System.Threading.Tasks;
using LmsKahoot.API.Data;
using LmsKahoot.API.Dtos;
using LmsKahoot.API.Models;
using LmsKahoot.API.Services;
using Microsoft.AspNet.SignalR;

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

        /// <summary>
        /// CLIENT -> SERVER (Teacher)
        /// Start a question by index (0-based) for the given session.
        /// Frontend teacher calls:
        ///   hubConnection.invoke("StartQuestion", sessionId, questionIndex)
        /// </summary>
        public async Task StartQuestion(int sessionId, int questionIndex)
        {
            // 1) Load session from DB
            var session = _context.QuizSessions.SingleOrDefault(s => s.SessionId == sessionId);
            if (session == null)
            {
                Clients.Caller.Error("Session not found.");
                return;
            }

            // 2) Load questions for the quiz
            var questions = _context.QuizQuestions
                .Where(q => q.QuizId == session.QuizId)
                .OrderBy(q => q.OrderIndex)
                .ToList();

            if (questionIndex < 0 || questionIndex >= questions.Count)
            {
                Clients.Caller.Error("Invalid question index.");
                return;
            }

            var question = questions[questionIndex];
            var timeLimitSeconds = question.TimeLimitSeconds;

            // 3) Update in-memory session state
            var state = SessionManager.Instance.StartQuestion(
                session.SessionId,
                question.QuestionId,
                questionIndex,
                timeLimitSeconds);

            var groupName = GetGroupName(session.SessionId);

            // 4) Broadcast to all participants in this session
            //    Frontend can use state.CurrentQuestionId and TimeLimitSeconds
            Clients.Group(groupName).QuestionStarted(state);
            await Task.CompletedTask;
        }

        /// <summary>
        /// CLIENT -> SERVER (Teacher)
        /// End the currently active question for a session.
        /// Frontend teacher calls:
        ///   hubConnection.invoke("EndQuestion", sessionId)
        /// </summary>
        public async Task EndQuestion(int sessionId)
        {
            var state = SessionManager.Instance.EndQuestion(sessionId);
            if (state == null)
            {
                Clients.Caller.Error("Session not found in memory.");
                return;
            }

            var groupName = GetGroupName(sessionId);

            // Notify everyone that the question has ended;
            // frontend can show "time up" and reveal leaderboard.
            Clients.Group(groupName).QuestionEnded(state);
            await Task.CompletedTask;
        }

        /// <summary>
        /// CLIENT -> SERVER (Student)
        /// Submit an answer for the current question.
        /// Frontend calls:
        ///   hubConnection.invoke("SubmitAnswer", sessionId, participantId, questionId, selectedOptionId)
        /// </summary>
        public async Task SubmitAnswer(int sessionId, int participantId, int questionId, int selectedOptionId)
        {
            var groupName = GetGroupName(sessionId);

            // 1) Get current state and validate timing + question
            var state = SessionManager.Instance.GetSessionState(sessionId);
            if (state == null)
            {
                Clients.Caller.AnswerRejected("Session not active.");
                return;
            }

            if (state.Status != SessionStatus.InProgress)
            {
                Clients.Caller.AnswerRejected("Question is not active.");
                return;
            }

            if (!state.CurrentQuestionId.HasValue || state.CurrentQuestionId.Value != questionId)
            {
                Clients.Caller.AnswerRejected("Invalid question for current session state.");
                return;
            }

            if (!state.QuestionStartUtc.HasValue)
            {
                Clients.Caller.AnswerRejected("Question start time not set.");
                return;
            }

            var nowUtc = DateTime.UtcNow;
            var elapsed = nowUtc - state.QuestionStartUtc.Value;
            var elapsedMs = (int)elapsed.TotalMilliseconds;

            if (elapsed.TotalSeconds > state.TimeLimitSeconds)
            {
                Clients.Caller.AnswerRejected("Time is up for this question.");
                return;
            }

            // 2) Check if this participant already answered this question in DB
            var existingAnswer = _context.ParticipantAnswers
                .SingleOrDefault(a =>
                    a.SessionId == sessionId &&
                    a.ParticipantId == participantId &&
                    a.QuestionId == questionId);

            if (existingAnswer != null)
            {
                Clients.Caller.AnswerRejected("You have already answered this question.");
                return;
            }

            // 3) Validate selected option and correctness
            var option = _context.QuizOptions
                .SingleOrDefault(o => o.OptionId == selectedOptionId && o.QuestionId == questionId);

            if (option == null)
            {
                Clients.Caller.AnswerRejected("Selected option is invalid.");
                return;
            }

            bool isCorrect = option.IsCorrect;

            // 4) Calculate score based on correctness + speed
            int scoreEarned = 0;
            if (isCorrect)
            {
                // Example scoring:
                // base 500 pts + up to 500 pts based on remaining time
                var totalMs = state.TimeLimitSeconds * 1000;
                var remainingMs = Math.Max(0, totalMs - elapsedMs);
                var timeFactor = (double)remainingMs / totalMs; // 0..1

                scoreEarned = 500 + (int)(500 * timeFactor);
            }

            // 5) Persist answer in DB
            var answer = new ParticipantAnswer
            {
                SessionId = sessionId,
                ParticipantId = participantId,
                QuestionId = questionId,
                SelectedOptionId = selectedOptionId,
                IsCorrect = isCorrect,
                ResponseTimeMs = elapsedMs,
                ScoreEarned = scoreEarned,
                CreatedAt = nowUtc
            };

            _context.ParticipantAnswers.Add(answer);
            _context.SaveChanges();

            // 6) Update in-memory scores / leaderboard
            var updatedState = SessionManager.Instance.ApplyAnswerScore(
                sessionId,
                participantId,
                elapsedMs,
                scoreEarned);

            // 7) Notify caller and all participants
            Clients.Caller.AnswerAccepted(new
            {
                IsCorrect = isCorrect,
                ScoreEarned = scoreEarned
            });

            if (updatedState != null)
            {
                Clients.Group(groupName).LeaderboardUpdated(updatedState.Leaderboard);
            }

            await Task.CompletedTask;
        }

        private static string GetGroupName(int sessionId)
        {
            return $"session-{sessionId}";
        }
    }
}
