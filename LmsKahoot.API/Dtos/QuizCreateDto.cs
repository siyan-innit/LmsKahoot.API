using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LmsKahoot.API.Dtos
{
    public class QuizCreateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int? CourseId { get; set; }
        public int CreatedByUserId { get; set; }
        public List<QuizQuestionCreateDto> Questions { get; set; }
    }

    public class QuizQuestionCreateDto
    {
        public string QuestionText { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int OrderIndex { get; set; }
        public List<QuizOptionCreateDto> Options { get; set; }
    }

    public class QuizOptionCreateDto
    {
        public string OptionText { get; set; }
        public bool IsCorrect { get; set; }
    }
}