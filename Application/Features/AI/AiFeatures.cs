using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Features.AI;

// ── Shared ────────────────────────────────────────────────────────────────────

public static class GeminiSystems
{
    public const string SalesAssistant =
        "You are a professional B2B sales assistant for a software company " +
        "selling tech solutions to tourism, travel, Hajj and Umrah companies. " +
        "Always be concise and professional. " +
        "Respond ONLY with valid JSON — no markdown, no extra text.";
}

// ═══════════════════════════════════════════════════════════════════════════════
// 1. Email Composer
// ═══════════════════════════════════════════════════════════════════════════════

public record ComposeEmailRequest(
    Guid   CompanyId,
    string Purpose,   // follow_up | proposal | renewal | introduction | demo_invite
    string Language,  // ar | en
    string Tone       // professional | friendly | formal
);

public record ComposeEmailResult(string Subject, string Body, string SuggestedSendTime);

public record ComposeEmailCommand(ComposeEmailRequest Request, Guid CurrentUserId)
    : IRequest<ComposeEmailResult>;

public class ComposeEmailHandler(IAppDbContext context, IGeminiService gemini)
    : IRequestHandler<ComposeEmailCommand, ComposeEmailResult>
{
    public async Task<ComposeEmailResult> Handle(ComposeEmailCommand cmd, CancellationToken ct)
    {
        var r  = cmd.Request;
        var co = await context.Companies
            .Include(c => c.Activities.OrderByDescending(a => a.CreatedAt).Take(1))
            .FirstOrDefaultAsync(c => c.Id == r.CompanyId, ct)
            ?? throw new KeyNotFoundException("Company not found.");

        var lastActivity = co.Activities.FirstOrDefault();
        var prompt =
            $"Write a {r.Purpose} email in language '{r.Language}', tone '{r.Tone}'.\n" +
            $"Company (English): {co.EnglishName}\n" +
            $"Company (Arabic): {co.ArabicName}\n" +
            $"Country: {co.Country}\n" +
            $"Stage: {co.Stage}\n" +
            $"Last Activity: {lastActivity?.Type.ToString() ?? "None"} on {lastActivity?.CreatedAt:yyyy-MM-dd}\n" +
            "Respond with JSON only: { \"subject\": \"...\", \"body\": \"...\", \"suggestedSendTime\": \"...\" }";

        var raw = await gemini.GenerateAsync(GeminiSystems.SalesAssistant, prompt, ct);
        
        try
        {
            var jsonString = CleanJson(raw);
            var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(jsonString);
            
            if (jsonNode is not System.Text.Json.Nodes.JsonObject jsonObj)
                throw new InvalidOperationException("AI response is not a valid JSON object.");

            string GetVal(string key)
            {
                var prop = jsonObj.FirstOrDefault(p => p.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
                if (prop == null) return string.Empty;
                
                // If the LLM returned an object or array instead of a string, stringify it
                return prop.GetValueKind() == JsonValueKind.String 
                    ? prop.GetValue<string>() 
                    : prop.ToJsonString();
            }

            return new ComposeEmailResult(
                GetVal("subject"),
                GetVal("body"),
                GetVal("suggestedSendTime")
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse AI response. Raw Output: {raw}", ex);
        }
    }

    private static string CleanJson(string s) => s.Trim().TrimStart('`').TrimEnd('`').Replace("```json", "").Replace("```", "").Trim();
}

// ═══════════════════════════════════════════════════════════════════════════════
// 2. Lead Scoring
// ═══════════════════════════════════════════════════════════════════════════════

public record LeadScoreFactor(string Factor, string Impact);
public record LeadScoreResult(Guid CompanyId, int Score, string Grade, string Summary,
    IEnumerable<LeadScoreFactor> Factors, string Recommendation);

public record GetLeadScoreQuery(Guid CompanyId, Guid CurrentUserId) : IRequest<LeadScoreResult>;

public class GetLeadScoreHandler(IAppDbContext context, IGeminiService gemini)
    : IRequestHandler<GetLeadScoreQuery, LeadScoreResult>
{
    public async Task<LeadScoreResult> Handle(GetLeadScoreQuery q, CancellationToken ct)
    {
        var co = await context.Companies
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(c => c.Id == q.CompanyId, ct)
            ?? throw new KeyNotFoundException("Company not found.");

        var daysSinceLast = co.Activities.Any()
            ? (DateTime.UtcNow - co.Activities.Max(a => a.CreatedAt)).Days
            : 999;

        var prompt =
            $"Score this B2B sales lead from 0 to 100 and explain.\n" +
            $"Stage: {co.Stage}\n" +
            $"AccountType: {co.AccountType}\n" +
            $"LeadStatus: {co.LeadStatus}\n" +
            $"Activities: {co.Activities.Count} total (Meetings: {co.Activities.Count(a => a.Type.ToString() == "Meeting")})\n" +
            $"Days since last activity: {daysSinceLast}\n" +
            $"ExpectedRevenue: {co.ExpectedRevenue?.ToString("N0") ?? "Unknown"}\n" +
            "Respond with JSON: { \"score\": 0-100, \"grade\": \"A/B/C\", \"summary\": \"...\", " +
            "\"factors\": [{\"factor\":\"...\",\"impact\":\"+/-N\"}], \"recommendation\": \"...\" }";

        var raw  = await gemini.GenerateAsync(GeminiSystems.SalesAssistant, prompt, ct);
        var doc  = JsonDocument.Parse(CleanJson(raw)).RootElement;

        var factors = doc.GetProperty("factors").EnumerateArray()
            .Select(f => new LeadScoreFactor(f.GetProperty("factor").GetString()!, f.GetProperty("impact").GetString()!));

        return new LeadScoreResult(
            q.CompanyId,
            doc.GetProperty("score").GetInt32(),
            doc.GetProperty("grade").GetString()!,
            doc.GetProperty("summary").GetString()!,
            factors,
            doc.GetProperty("recommendation").GetString()!);
    }

    private static string CleanJson(string s) => s.Trim().TrimStart('`').TrimEnd('`').Replace("```json", "").Replace("```", "").Trim();
}

// ═══════════════════════════════════════════════════════════════════════════════
// 3. Next Action Suggestion
// ═══════════════════════════════════════════════════════════════════════════════

public record NextActionResult(string SuggestedAction, string Reason, string Urgency,
    string SuggestedDate, string MessageTemplate);

public record GetNextActionQuery(Guid CompanyId, Guid CurrentUserId) : IRequest<NextActionResult>;

public class GetNextActionHandler(IAppDbContext context, IGeminiService gemini)
    : IRequestHandler<GetNextActionQuery, NextActionResult>
{
    public async Task<NextActionResult> Handle(GetNextActionQuery q, CancellationToken ct)
    {
        var co = await context.Companies
            .Include(c => c.Activities.OrderByDescending(a => a.CreatedAt).Take(3))
            .FirstOrDefaultAsync(c => c.Id == q.CompanyId, ct)
            ?? throw new KeyNotFoundException("Company not found.");

        var recentActs = string.Join(", ", co.Activities.Select(a => $"{a.Type} on {a.CreatedAt:yyyy-MM-dd}"));

        var prompt =
            $"Suggest the best next action for this sales lead.\n" +
            $"Stage: {co.Stage}\n" +
            $"Recent activities: {(string.IsNullOrEmpty(recentActs) ? "None" : recentActs)}\n" +
            $"LeadStatus: {co.LeadStatus}\n" +
            $"Today: {DateTime.UtcNow:yyyy-MM-dd}\n" +
            "Respond with JSON: { \"suggestedAction\": \"...\", \"reason\": \"...\", \"urgency\": \"High/Medium/Low\", " +
            "\"suggestedDate\": \"YYYY-MM-DD\", \"messageTemplate\": \"...\" }";

        var raw = await gemini.GenerateAsync(GeminiSystems.SalesAssistant, prompt, ct);
        
        try
        {
            var jsonString = CleanJson(raw);
            var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(jsonString);
            
            if (jsonNode is not System.Text.Json.Nodes.JsonObject jsonObj)
                throw new InvalidOperationException("AI response is not a valid JSON object.");

            string GetVal(string key)
            {
                var prop = jsonObj.FirstOrDefault(p => p.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
                if (prop == null) return string.Empty;
                
                return prop.GetValueKind() == JsonValueKind.String 
                    ? prop.GetValue<string>() 
                    : prop.ToJsonString();
            }

            return new NextActionResult(
                GetVal("suggestedAction"),
                GetVal("reason"),
                GetVal("urgency"),
                GetVal("suggestedDate"),
                GetVal("messageTemplate")
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse AI response. Raw Output: {raw}", ex);
        }
    }

    private static string CleanJson(string s) => s.Trim().TrimStart('`').TrimEnd('`').Replace("```json", "").Replace("```", "").Trim();
}

// ═══════════════════════════════════════════════════════════════════════════════
// 4. Sales Insights Dashboard
// ═══════════════════════════════════════════════════════════════════════════════

public record InsightsHighlight(string Text);
public record InsightsAlert(string Text);
public record SalesInsightsResult(string SummaryEn, string SummaryAr,
    IEnumerable<string> Highlights, IEnumerable<string> Alerts);

public record GetSalesInsightsQuery(DateTime From, DateTime To) : IRequest<SalesInsightsResult>;

public class GetSalesInsightsHandler(IAppDbContext context, IGeminiService gemini)
    : IRequestHandler<GetSalesInsightsQuery, SalesInsightsResult>
{
    public async Task<SalesInsightsResult> Handle(GetSalesInsightsQuery q, CancellationToken ct)
    {
        var orders = await context.SalesOrders
            .Where(o => o.CreatedAt >= q.From && o.CreatedAt <= q.To)
            .Include(o => o.Company).Include(o => o.CreatedBy)
            .ToListAsync(ct);

        var totalUsd     = orders.Sum(o => o.UsdAmount);
        var byCountry    = orders.GroupBy(o => o.Company?.Country ?? "?").Select(g => $"{g.Key}: ${g.Sum(o => o.UsdAmount):N0}");
        var bySales      = orders.GroupBy(o => o.CreatedBy?.Name  ?? "?").Select(g => $"{g.Key}: {g.Count()} deals");
        var stuckCompanies = await context.Companies
            .Where(c => c.IsActive && c.Activities.All(a => a.CreatedAt < DateTime.UtcNow.AddDays(-30)))
            .CountAsync(ct);

        var prompt =
            $"Generate a sales insights summary for period {q.From:yyyy-MM-dd} to {q.To:yyyy-MM-dd}.\n" +
            $"Total revenue USD: ${totalUsd:N0}\n" +
            $"Revenue by country: {string.Join("; ", byCountry)}\n" +
            $"Deals by sales rep: {string.Join("; ", bySales)}\n" +
            $"Companies with no contact in 30+ days: {stuckCompanies}\n" +
            "Respond with JSON: { \"summaryEn\": \"...\", \"summaryAr\": \"...\", " +
            "\"highlights\": [\"...\"], \"alerts\": [\"...\"] }";

        var raw = await gemini.GenerateAsync(GeminiSystems.SalesAssistant, prompt, ct);
        var doc = JsonDocument.Parse(CleanJson(raw)).RootElement;

        return new SalesInsightsResult(
            doc.GetProperty("summaryEn").GetString()!,
            doc.GetProperty("summaryAr").GetString()!,
            doc.GetProperty("highlights").EnumerateArray().Select(h => h.GetString()!),
            doc.GetProperty("alerts").EnumerateArray().Select(a => a.GetString()!));
    }

    private static string CleanJson(string s) => s.Trim().TrimStart('`').TrimEnd('`').Replace("```json", "").Replace("```", "").Trim();
}
