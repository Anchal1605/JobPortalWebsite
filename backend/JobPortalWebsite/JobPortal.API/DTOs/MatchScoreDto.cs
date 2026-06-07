namespace JobPortal.API.DTOs;

public class MatchScoreDto
{
    public int Score { get; set; }
    public List<string> Strengths { get; set; } = [];
    public List<string> Gaps { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
    public string Tip { get; set; } = string.Empty;
}
