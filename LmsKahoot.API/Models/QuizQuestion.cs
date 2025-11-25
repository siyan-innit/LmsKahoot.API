using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace LmsKahoot.API.Models
{
    [Table("QuizQuestion")]
    public class QuizQuestion
    {
        [Key]
        public int QuestionId { get; set; }
        public int QuizId { get; set; }
        public string QuestionText { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int OrderIndex { get; set; }

        public virtual Quiz Quiz { get; set; }
        public virtual ICollection<QuizOption> Options { get; set; }
    }
}