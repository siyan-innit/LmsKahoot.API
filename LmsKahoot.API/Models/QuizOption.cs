using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LmsKahoot.API.Models
{
    [Table("QuizOption")]
    public class QuizOption
    {
        [Key]
        public int OptionId { get; set; }
        public int QuestionId { get; set; }
        public string OptionText { get; set; }
        public bool IsCorrect { get; set; }

        public virtual QuizQuestion Question { get; set; }
    }
}