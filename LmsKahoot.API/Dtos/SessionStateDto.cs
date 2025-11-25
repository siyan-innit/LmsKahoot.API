using System;
using System.Collections.Generic;

namespace LmsKahoot.API.Dtos
{
    public class SessionStateDto
    {
        public int SessionId { get; set; }          // INT not GUID
        public SessionStatus Status { get; set; }   // ENUM not string

        public int CurrentQuestionIndex { get; set; }
        public int? CurrentQuestionId { get; set; }
        public DateTime? QuestionStartUtc { get; set; }
        public int TimeLimitSeconds { get; set; }

        public List<ParticipantDto> Participants { get; set; }
        public List<LeaderboardEntryDto> Leaderboard { get; set; }

        public DateTime ServerUtcNow { get; set; }
    }
}
