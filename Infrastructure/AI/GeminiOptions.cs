namespace Infrastructure.AI;

public class GeminiOptions
{
    public string GeminiApiKey { get; set; } = string.Empty;
    public string Model        { get; set; } = "gemini-2.0-flash";
    public int    MaxTokens    { get; set; } = 1000;
}
