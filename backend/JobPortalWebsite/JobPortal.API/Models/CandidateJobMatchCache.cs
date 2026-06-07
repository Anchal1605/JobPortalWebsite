namespace JobPortal.API.Models;

public class CandidateJobMatchCache
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int JobId { get; set; }
    public DateTime ProfileUpdatedAt { get; set; }
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    public int Score { get; set; }
    public string? StrengthsJson { get; set; }
    public string? GapsJson { get; set; }
    public string? Summary { get; set; }
    public string? Tip { get; set; }
}
