using LmsKahoot.API.Data;
using LmsKahoot.API.Dtos;
using LmsKahoot.API.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;

namespace LmsKahoot.API.Controllers
{
    [RoutePrefix("api/quizzes")]
    public class QuizController : ApiController
    {
        private readonly LmsKahootContext _context = new LmsKahootContext();

        // GET api/quizzes?courseId=123
        [HttpGet, Route("")]
        public IHttpActionResult GetQuizzes(int? courseId = null)
        {
            var query = _context.Quizzes.AsQueryable();

            if (courseId.HasValue)
            {
                query = query.Where(q => q.CourseId == courseId.Value);
            }

            var quizzes = query
                .OrderByDescending(q => q.CreatedDate)
                .Select(q => new
                {
                    q.QuizId,
                    q.Title,
                    q.Description,
                    q.CourseId,
                    q.CreatedByUserId,
                    q.CreatedDate
                })
                .ToList();

            return Ok(quizzes);
        }

        // GET api/quizzes/5
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult GetQuiz(int id)
        {
            var quiz = _context.Quizzes
                .Include(q => q.Questions.Select(qq => qq.Options))
                .SingleOrDefault(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            var dto = new QuizDto
            {
                QuizId = quiz.QuizId,
                Title = quiz.Title,
                Description = quiz.Description,
                CourseId = quiz.CourseId,
                CreatedByUserId = quiz.CreatedByUserId,
                Questions = quiz.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new QuizQuestionDto
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        TimeLimitSeconds = q.TimeLimitSeconds,
                        OrderIndex = q.OrderIndex,
                        Options = q.Options
                            .Select(o => new QuizOptionDto
                            {
                                OptionId = o.OptionId,
                                OptionText = o.OptionText,
                                IsCorrect = o.IsCorrect
                            }).ToList()
                    }).ToList()
            };

            return Ok(dto);
        }

        // POST api/quizzes
        [HttpPost, Route("")]
        public IHttpActionResult CreateQuiz([FromBody] QuizCreateDto model)
        {
            if (model == null)
                return BadRequest("Invalid payload.");

            var quiz = new Quiz
            {
                Title = model.Title,
                Description = model.Description,
                CourseId = model.CourseId,
                CreatedByUserId = model.CreatedByUserId,
                CreatedDate = DateTime.UtcNow,
                Questions = model.Questions?.Select(q => new QuizQuestion
                {
                    QuestionText = q.QuestionText,
                    TimeLimitSeconds = q.TimeLimitSeconds,
                    OrderIndex = q.OrderIndex,
                    Options = q.Options?.Select(o => new QuizOption
                    {
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                }).ToList()
            };

            _context.Quizzes.Add(quiz);
            _context.SaveChanges();

            // returns 201 + new quiz id
            return Created($"api/quizzes/{quiz.QuizId}", new { quiz.QuizId });
        }

        // PUT api/quizzes/5
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult UpdateQuiz(int id, [FromBody] QuizCreateDto model)
        {
            if (model == null)
                return BadRequest("Invalid payload.");

            var quiz = _context.Quizzes
                .Include(q => q.Questions.Select(qq => qq.Options))
                .SingleOrDefault(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            quiz.Title = model.Title;
            quiz.Description = model.Description;
            quiz.CourseId = model.CourseId;

            // For v1: delete and recreate questions & options (simpler)
            var existingQuestions = quiz.Questions.ToList();
            foreach (var q in existingQuestions)
            {
                _context.QuizOptions.RemoveRange(q.Options.ToList());
                _context.QuizQuestions.Remove(q);
            }

            quiz.Questions = model.Questions?.Select(q => new QuizQuestion
            {
                QuestionText = q.QuestionText,
                TimeLimitSeconds = q.TimeLimitSeconds,
                OrderIndex = q.OrderIndex,
                Options = q.Options?.Select(o => new QuizOption
                {
                    OptionText = o.OptionText,
                    IsCorrect = o.IsCorrect
                }).ToList()
            }).ToList();

            _context.SaveChanges();

            return Ok();
        }

        // DELETE api/quizzes/5
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult DeleteQuiz(int id)
        {
            var quiz = _context.Quizzes
                .Include(q => q.Questions.Select(qq => qq.Options))
                .SingleOrDefault(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            foreach (var question in quiz.Questions.ToList())
            {
                _context.QuizOptions.RemoveRange(question.Options.ToList());
                _context.QuizQuestions.Remove(question);
            }

            _context.Quizzes.Remove(quiz);
            _context.SaveChanges();

            return Ok();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
