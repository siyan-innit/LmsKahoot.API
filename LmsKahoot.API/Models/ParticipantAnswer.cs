using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LmsKahoot.API.Models
{
    [Table("ParticipantAnswer")]
    public class ParticipantAnswer
    {
        [Key]
        public int AnswerId { get; set; }
        public int SessionId { get; set; }
        public int ParticipantId { get; set; }
        public int QuestionId { get; set; }
        public int SelectedOptionId { get; set; }
        public bool IsCorrect { get; set; }
        public int ResponseTimeMs { get; set; }
        public int ScoreEarned { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual QuizSession Session { get; set; }
        public virtual QuizSessionParticipant Participant { get; set; }
        public virtual QuizQuestion Question { get; set; }
        
    }
}