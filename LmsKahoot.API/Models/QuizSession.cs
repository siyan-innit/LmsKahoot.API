using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LmsKahoot.API.Models
{
    [Table("QuizSession")]
    public class QuizSession
    {
        [Key]
        public int SessionId { get; set; }
        public int QuizId { get; set; }
        public int HostUserId { get; set; }
        public string SessionCode { get; set; }
        public string Status { get; set; } // Lobby / InProgress / Completed
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        public virtual Quiz Quiz { get; set; }
        public virtual ICollection<QuizSessionParticipant> Participants { get; set; }
    }
}