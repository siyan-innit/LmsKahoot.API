using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LmsKahoot.API.Models
{
    [Table("QuizSessionParticipant")]
    public class QuizSessionParticipant
    {
        [Key]
        public int ParticipantId { get; set; }
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public DateTime JoinedAt { get; set; }
        public int TotalScore { get; set; }
        public int? AverageResponseTimeMs { get; set; }

        public virtual QuizSession Session { get; set; }
        public virtual ICollection<ParticipantAnswer> Answers { get; set; }
    }
}