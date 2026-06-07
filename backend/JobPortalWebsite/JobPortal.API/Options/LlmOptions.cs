namespace JobPortal.API.Options;

public class LlmOptions
{
    public const string SectionName = "Llm";
    public string Provider { get; set; } = "Gemini";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-flash";

}
