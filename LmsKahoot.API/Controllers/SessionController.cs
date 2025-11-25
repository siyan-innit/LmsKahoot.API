using System;
using System.Linq;
using System.Web.Http;
using LmsKahoot.API.Data;
using LmsKahoot.API.Dtos;
using LmsKahoot.API.Models;
using LmsKahoot.API.Services;

namespace LmsKahoot.API.Controllers
{
    [RoutePrefix("api/sessions")]
    public class SessionController : ApiController
    {
        private readonly LmsKahootContext _context = new LmsKahootContext();

        /// <summary>
        /// Creates a new live session for a given quiz.
        /// Example:
        ///   POST api/sessions/create?quizId=1&hostUserId=10
        /// Returns:
        ///   { sessionId, sessionCode, state { ...SessionStateDto... } }
        /// </summary>
        [HttpPost]
        [Route("create")]
        public IHttpActionResult CreateSession(int quizId, int hostUserId)
        {
            // 1) Validate quiz exists
            var quiz = _context.Quizzes.SingleOrDefault(q => q.QuizId == quizId);
            if (quiz == null)
            {
                return NotFound(); // 404
            }

            // 2) Generate a simple numeric session code like "483921"
            var random = new Random();
            var sessionCode = random.Next(100000, 999999).ToString();

            // 3) Create DB row
            var session = new QuizSession
            {
                QuizId = quizId,
                HostUserId = hostUserId,
                SessionCode = sessionCode,
                Status = "Lobby",
                CreatedAt = DateTime.UtcNow,
                StartedAt = null,
                EndedAt = null
            };

            _context.QuizSessions.Add(session);
            _context.SaveChanges(); // sets SessionId

            // 4) Initialize in-memory state
            SessionStateDto state = SessionManager.Instance.InitializeSession(session.SessionId);

            // 5) Return data to caller (teacher UI)
            return Ok(new
            {
                SessionId = session.SessionId,
                SessionCode = session.SessionCode,
                State = state
            });
        }

        /// <summary>
        /// Simple helper to get current state snapshot for debugging / testing.
        /// GET api/sessions/{sessionId}/state
        /// </summary>
        [HttpGet]
        [Route("{sessionId:int}/state")]
        public IHttpActionResult GetState(int sessionId)
        {
            var state = SessionManager.Instance.GetSessionState(sessionId);
            if (state == null)
            {
                return NotFound();
            }

            return Ok(state);
        }
    }
}
