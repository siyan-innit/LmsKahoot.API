using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LmsKahoot.API.Dtos
{
    public class QuizDto
    {
        public int QuizId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? CourseId { get; set; }
        public int CreatedByUserId { get; set; }

        public List<QuizQuestionDto> Questions { get; set; }
    }

    public class QuizQuestionDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int OrderIndex { get; set; }
        public List<QuizOptionDto> Options { get; set; }
    }

    public class QuizOptionDto
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; }
        public bool IsCorrect { get; set; } // later we can hide this from students if needed
    }
}