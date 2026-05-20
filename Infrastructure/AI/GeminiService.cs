using Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.AI;

public class GeminiService(HttpClient http, IOptions<GeminiOptions> opts) : IGeminiService
{
    private readonly GeminiOptions _opts = opts.Value;

    public async Task<string> GenerateAsync(string systemInstruction, string userMessage, CancellationToken ct = default)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/" +
                  $"{_opts.Model}:generateContent?key={_opts.GeminiApiKey}";

        var body = new
        {
            system_instruction = new { parts = new[] { new { text = systemInstruction } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userMessage } } }
            },
            generationConfig = new
            {
                maxOutputTokens = _opts.MaxTokens,
                temperature     = 0.7
            }
        };

        int maxRetries = 2;
        int delayMs = 1500;

        for (int i = 0; i <= maxRetries; i++)
        {
            var response = await http.PostAsJsonAsync(url, body, ct);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<GeminiApiResponse>(json);
                return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                       ?? string.Empty;
            }

            if ((int)response.StatusCode == 429 && i < maxRetries)
            {
                // Wait and retry with exponential backoff
                await Task.Delay(delayMs, ct);
                delayMs *= 2;
                continue;
            }

            // Break out of the loop and try the fallback
            break;
        }

        // ─── GROQ FALLBACK ───────────────────────────────────────────────
        var groqUrl = "https://api.groq.com/openai/v1/chat/completions";
        var groqBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "system", content = systemInstruction },
                new { role = "user", content = userMessage }
            },
            temperature = 0.7,
            max_completion_tokens = _opts.MaxTokens > 0 ? _opts.MaxTokens : 1024
        };

        using var groqRequest = new HttpRequestMessage(HttpMethod.Post, groqUrl);
        groqRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "GROQ_API_KEY");
        groqRequest.Content = JsonContent.Create(groqBody);

        var groqResponse = await http.SendAsync(groqRequest, ct);
        groqResponse.EnsureSuccessStatusCode();

        var groqJson = await groqResponse.Content.ReadAsStringAsync(ct);
        var groqResult = JsonSerializer.Deserialize<GroqApiResponse>(groqJson);
        return groqResult?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    // ── Internal response shape ───────────────────────────────────────────────
    private sealed class GeminiApiResponse
    {
        [JsonPropertyName("candidates")] public List<GeminiCandidate>? Candidates { get; set; }
    }
    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")] public GeminiContent? Content { get; set; }
    }
    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")] public List<GeminiPart>? Parts { get; set; }
    }
    private sealed class GeminiPart
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
    }

    // ── Groq response shape ───────────────────────────────────────────────────
    private sealed class GroqApiResponse
    {
        [JsonPropertyName("choices")] public List<GroqChoice>? Choices { get; set; }
    }
    private sealed class GroqChoice
    {
        [JsonPropertyName("message")] public GroqMessage? Message { get; set; }
    }
    private sealed class GroqMessage
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}
