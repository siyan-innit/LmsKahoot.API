using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LmsKahoot.API.Dtos
{
    public class LeaderboardEntryDto
    {
        public int ParticipantId { get; set; }
        public string DisplayName { get; set; }
        public int TotalScore { get; set; }
        public int Rank { get; set; }
        public int? AverageResponseTimeMs { get; set; }
    }
}