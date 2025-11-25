using LmsKahoot.API.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace LmsKahoot.API.Data
{
    public class LmsKahootContext : DbContext
    {
        public LmsKahootContext()
            : base("LmsKahootConnection") // this name must match Web.config
        {
        }

        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizOption> QuizOptions { get; set; }
        public DbSet<QuizSession> QuizSessions { get; set; }
        public DbSet<QuizSessionParticipant> QuizSessionParticipants { get; set; }
        public DbSet<ParticipantAnswer> ParticipantAnswers { get; set; }
    }
}