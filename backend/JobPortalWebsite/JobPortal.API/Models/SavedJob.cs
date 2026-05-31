namespace JobPortal.API.Models
{
    public class SavedJob
    {
        public int Id { get; set; }
        public int CandidateUserId { get; set; } // no FK, service-layer validation
        public int JobId { get; set; } // no FK, service-layer validation
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
