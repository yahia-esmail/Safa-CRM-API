namespace Application.Common.Interfaces;

public interface IGeminiService
{
    /// <summary>Sends a prompt to Gemini and returns the raw text response.</summary>
    Task<string> GenerateAsync(string systemInstruction, string userMessage, CancellationToken ct = default);
}
