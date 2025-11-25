using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LmsKahoot.API.Models
{
    [Table("Quiz")]
    public class Quiz
    {
        [Key]
        public int QuizId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? CourseId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }

        public virtual ICollection<QuizQuestion> Questions { get; set; }
    }
}