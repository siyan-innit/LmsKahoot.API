namespace LmsKahoot.API.Dtos
{
    public class ParticipantDto
    {
        public int ParticipantId { get; set; }
        public string DisplayName { get; set; }
        public int TotalScore { get; set; }
        public int? AverageResponseTimeMs { get; set; }

        // Used for reconnect / presence – SessionManager sets this
        public bool IsConnected { get; set; }
    }
}
