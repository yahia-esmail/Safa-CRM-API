using MediatR;

namespace Application.Features.Companies.Import;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record ImportRowDto(
    int    RowNumber,
    string EnglishName,
    string ArabicName,
    string Country,
    string Phone,
    string Email,
    string? Website,
    int?   SafaKey,
    string AccountType,
    string Stage,
    string? LeadSource,
    string LeadStatus,
    decimal? ExpectedRevenue,
    string? AssignedToEmail,
    string? Notes);

public record ProblemRowDto(
    int     RowNumber,
    string  Status,     // MissingRequired | InvalidFormat | DuplicateInFile | DuplicateInDb
    string  Field,
    string? Value,
    string  Reason,
    string? AiSuggestion = null,
    double? AiConfidence = null);

public record ImportPreviewResult(
    string                  ImportId,
    int                     TotalRows,
    int                     ValidCount,
    int                     DuplicateInFileCount,
    int                     DuplicateInDbCount,
    int                     InvalidCount,
    IEnumerable<ImportRowDto>     ValidRows,
    IEnumerable<ProblemRowDto>    ProblemRows);

public record ImportConfirmResult(
    int    ImportedCount,
    int    SkippedCount,
    string Message);

public record ImportLogDto(
    Guid     Id,
    string   FileName,
    string   Status,
    int      TotalRows,
    int      ImportedCount,
    int      SkippedCount,
    string   UploadedBy,
    DateTime UploadedAt);

// ── Commands & Queries ────────────────────────────────────────────────────────

public record PreviewImportCommand(
    Stream  FileStream,
    string  FileName,
    Guid    CurrentUserId)
    : IRequest<ImportPreviewResult>;

public record ConfirmImportCommand(
    string ImportId,
    Guid   CurrentUserId)
    : IRequest<ImportConfirmResult>;

public record GetImportLogsQuery : IRequest<IEnumerable<ImportLogDto>>;
