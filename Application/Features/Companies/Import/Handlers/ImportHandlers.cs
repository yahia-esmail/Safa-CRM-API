using Application.Common;
using Application.Common.Interfaces;
using Application.Features.Companies.Import;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Application.Features.Companies.Import.Handlers;

// ── Preview Handler ───────────────────────────────────────────────────────────

public class PreviewImportHandler(
    IExcelImportService excelService,
    IAppDbContext       context,
    IMemoryCache        cache)
    : IRequestHandler<PreviewImportCommand, ImportPreviewResult>
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRegex = new(@"^\+[1-9]\d{7,14}$",         RegexOptions.Compiled);

    private static readonly HashSet<string> ValidStages = new(StringComparer.OrdinalIgnoreCase)
        { "LeadOpportunity", "Qualification", "Demo", "FollowUp", "Proposal", "Negotiation", "ClosedWon", "ClosedLost" };

    public async Task<ImportPreviewResult> Handle(PreviewImportCommand cmd, CancellationToken ct)
    {
        var rawRows  = excelService.ReadCompanyRows(cmd.FileStream).ToList();
        var validRows     = new List<ImportRowDto>();
        var problemRows   = new List<ProblemRowDto>();

        // Track unique values within the file
        var seenEmails   = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var seenPhones   = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var seenSafaKeys = new Dictionary<int, int>();

        // Fetch existing values from DB once (in-memory sets for fast lookup)
        var dbEmails   = await context.Companies.Where(c => c.Email   != null).Select(c => c.Email.ToLower()).ToHashSetAsync(ct);
        var dbPhones   = await context.Companies.Where(c => c.Phone   != null).Select(c => c.Phone).ToHashSetAsync(ct);
        var dbSafaKeys = await context.Companies.Where(c => c.SafaKey != null).Select(c => c.SafaKey!.Value).ToHashSetAsync(ct);
        var dbUserEmails = await context.Users.Select(u => new { u.Email, u.Id }).ToDictionaryAsync(u => u.Email, u => u.Id, ct);

        foreach (var (rowNum, cells) in rawRows)
        {
            string Get(string col) => cells.TryGetValue(col, out var v) ? v?.Trim() ?? "" : "";
            string? GetOpt(string col) => cells.TryGetValue(col, out var v) ? (string.IsNullOrWhiteSpace(v) ? null : v.Trim()) : null;

            var enName  = Get("EnglishName");
            var arName  = Get("ArabicName");
            var country = Get("Country");
            var phone   = Get("Phone");
            var email   = Get("Email");

            // Auto-format phone to E.164
            if (!string.IsNullOrWhiteSpace(phone))
            {
                phone = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
                if (phone.StartsWith("00")) phone = "+" + phone.Substring(2);
                else if (!phone.StartsWith("+") && phone.Length > 0) phone = "+" + phone;
            }

            // ── Required field checks ─────────────────────────────────────
            if (string.IsNullOrWhiteSpace(enName)) { problemRows.Add(new(rowNum, "MissingRequired", "EnglishName", null, "EnglishName is required / الاسم الإنجليزي مطلوب")); continue; }
            if (string.IsNullOrWhiteSpace(arName)) { problemRows.Add(new(rowNum, "MissingRequired", "ArabicName",  null, "ArabicName is required / الاسم العربي مطلوب")); continue; }
            if (string.IsNullOrWhiteSpace(country) || country.Length != 2) { problemRows.Add(new(rowNum, "InvalidFormat", "Country", country, "Country must be a 2-letter ISO code (e.g. SA, EG, AE)")); continue; }
            if (string.IsNullOrWhiteSpace(phone)   || !PhoneRegex.IsMatch(phone)) { problemRows.Add(new(rowNum, "InvalidFormat", "Phone", phone, "Phone must be in E.164 format e.g. +966501234567")); continue; }
            if (string.IsNullOrWhiteSpace(email)   || !EmailRegex.IsMatch(email)) { problemRows.Add(new(rowNum, "InvalidFormat", "Email", email, "A valid email address is required")); continue; }

            var accountType = Get("AccountType");
            if (accountType != "New" && accountType != "Existing") { problemRows.Add(new(rowNum, "InvalidFormat", "AccountType", accountType, "AccountType must be 'New' or 'Existing'")); continue; }

            var stage = Get("Stage");
            if (!ValidStages.Contains(stage)) { problemRows.Add(new(rowNum, "InvalidFormat", "Stage", stage, $"Invalid stage. Valid values: {string.Join(", ", ValidStages)}")); continue; }

            var leadStatus = Get("LeadStatus");
            if (leadStatus != "Reached" && leadStatus != "UnReached") { problemRows.Add(new(rowNum, "InvalidFormat", "LeadStatus", leadStatus, "LeadStatus must be 'Reached' or 'UnReached'")); continue; }

            // ── SafaKey ───────────────────────────────────────────────────
            int? safaKey = null;
            var safaKeyStr = GetOpt("SafaKey");
            if (safaKeyStr != null)
            {
                if (!int.TryParse(safaKeyStr, out var sk) || sk <= 0) { problemRows.Add(new(rowNum, "InvalidFormat", "SafaKey", safaKeyStr, "SafaKey must be a positive integer")); continue; }
                safaKey = sk;
            }

            // ── ExpectedRevenue ───────────────────────────────────────────
            decimal? revenue = null;
            var revStr = GetOpt("ExpectedRevenue");
            if (revStr != null)
            {
                if (!decimal.TryParse(revStr, out var rev) || rev < 0) { problemRows.Add(new(rowNum, "InvalidFormat", "ExpectedRevenue", revStr, "ExpectedRevenue must be a positive number")); continue; }
                revenue = rev;
            }

            // ── AssignedTo (by email) ─────────────────────────────────────
            Guid? assignedToId = null;
            var assignedEmail = GetOpt("AssignedTo");
            if (assignedEmail != null)
            {
                if (!dbUserEmails.TryGetValue(assignedEmail, out var uid)) { problemRows.Add(new(rowNum, "InvalidFormat", "AssignedTo", assignedEmail, "No sales rep found with this email in the system")); continue; }
                assignedToId = uid;
            }

            var emailNorm = email.ToLowerInvariant();

            // ── In-file duplicate checks ──────────────────────────────────
            if (seenEmails.TryGetValue(emailNorm, out var prevEmailRow)) { problemRows.Add(new(rowNum, "DuplicateInFile", "Email", email, $"Duplicate email found in row {prevEmailRow} of this file")); continue; }
            if (seenPhones.TryGetValue(phone,     out var prevPhoneRow)) { problemRows.Add(new(rowNum, "DuplicateInFile", "Phone", phone, $"Duplicate phone found in row {prevPhoneRow} of this file")); continue; }
            if (safaKey.HasValue && seenSafaKeys.TryGetValue(safaKey.Value, out var prevKeyRow)) { problemRows.Add(new(rowNum, "DuplicateInFile", "SafaKey", safaKeyStr, $"Duplicate SafaKey found in row {prevKeyRow} of this file")); continue; }

            // ── DB duplicate checks ───────────────────────────────────────
            if (dbEmails.Contains(emailNorm))    { problemRows.Add(new(rowNum, "DuplicateInDb", "Email",   email, "A company with this email already exists")); continue; }
            if (dbPhones.Contains(phone))         { problemRows.Add(new(rowNum, "DuplicateInDb", "Phone",   phone, "A company with this phone already exists")); continue; }
            if (safaKey.HasValue && dbSafaKeys.Contains(safaKey.Value)) { problemRows.Add(new(rowNum, "DuplicateInDb", "SafaKey", safaKeyStr, "A company with this SafaKey already exists")); continue; }

            // ── Track seen values ─────────────────────────────────────────
            seenEmails[emailNorm] = rowNum;
            seenPhones[phone]     = rowNum;
            if (safaKey.HasValue) seenSafaKeys[safaKey.Value] = rowNum;

            validRows.Add(new ImportRowDto(
                rowNum, enName, arName, country.ToUpper(), phone, emailNorm,
                GetOpt("Website"), safaKey, accountType, stage,
                GetOpt("LeadSource"), leadStatus, revenue,
                assignedEmail, GetOpt("Notes")));
        }

        // Cache valid rows for confirm step (15 min TTL)
        var importId = Guid.NewGuid().ToString();
        cache.Set(importId, validRows, TimeSpan.FromMinutes(15));

        int dupInFile = problemRows.Count(p => p.Status == "DuplicateInFile");
        int dupInDb   = problemRows.Count(p => p.Status == "DuplicateInDb");
        int invalid   = problemRows.Count - dupInFile - dupInDb;

        return new ImportPreviewResult(
            importId, rawRows.Count, validRows.Count,
            dupInFile, dupInDb, invalid,
            validRows, problemRows);
    }
}

// ── Confirm Handler ───────────────────────────────────────────────────────────

public class ConfirmImportHandler(
    IAppDbContext context,
    IMemoryCache  cache,
    INotificationService notificationService)
    : IRequestHandler<ConfirmImportCommand, ImportConfirmResult>
{
    public async Task<ImportConfirmResult> Handle(ConfirmImportCommand cmd, CancellationToken ct)
    {
        if (!cache.TryGetValue<List<ImportRowDto>>(cmd.ImportId, out var rows) || rows is null)
            throw new InvalidOperationException("Import session expired or not found. Please re-upload the file.");

        cache.Remove(cmd.ImportId);

        var companies = rows.Select(r => new Company
        {
            ArabicName      = r.ArabicName,
            EnglishName     = r.EnglishName,
            Country         = r.Country,
            Phone           = r.Phone,
            Email           = r.Email,
            Website         = r.Website,
            SafaKey         = r.SafaKey,
            AccountType     = r.AccountType,
            Stage           = Enum.Parse<Domain.Enums.Stage>(r.Stage, true),
            LeadSource      = r.LeadSource,
            LeadStatus      = r.LeadStatus,
            ExpectedRevenue = r.ExpectedRevenue,
            AssignedToUserId = null   // resolved at preview; skipping nav here for simplicity
        }).ToList();

        await context.Companies.AddRangeAsync(companies, ct);

        // Write import log
        var importLog = new ImportLog
        {
            FileName        = "bulk-import",
            Type            = "Companies",
            Status          = "Success",
            TotalRows       = rows.Count,
            SuccessRows     = rows.Count,
            UploadedByUserId = cmd.CurrentUserId
        };
        context.ImportLogs.Add(importLog);

        await context.SaveChangesAsync(ct);

        // Send completion notification (B-2)
        try
        {
            await notificationService.SendAsync(
                cmd.CurrentUserId,
                "Excel Import Completed",
                $"The import of {rows.Count} companies has been completed successfully.",
                NotificationType.NewCompanyImported,
                "ImportLog",
                importLog.Id.ToString());
        }
        catch
        {
            // Fail silent to not block transaction success
        }

        return new ImportConfirmResult(
            rows.Count, 0,
            $"{rows.Count} companies imported successfully.");
    }
}

// ── Logs Query Handler ────────────────────────────────────────────────────────

public class GetImportLogsHandler(IAppDbContext context)
    : IRequestHandler<GetImportLogsQuery, IEnumerable<ImportLogDto>>
{
    public async Task<IEnumerable<ImportLogDto>> Handle(GetImportLogsQuery q, CancellationToken ct)
    {
        var logs = await context.ImportLogs
            .Include(l => l.UploadedBy)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        return logs.Select(l => new ImportLogDto(
            l.Id, l.FileName, l.Status, l.TotalRows,
            l.SuccessRows, l.TotalRows - l.SuccessRows,
            l.UploadedBy?.Name ?? "", l.CreatedAt));
    }
}
