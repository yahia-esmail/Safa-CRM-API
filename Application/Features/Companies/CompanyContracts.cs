using Application.Common;
using Domain.Enums;
using MediatR;

namespace Application.Features.Companies;

// --- DTOs ---
public record CompanyDto(
    Guid Id,
    string ArabicName,
    string EnglishName,
    string Country,
    string Phone,
    string Email,
    string? Website,
    int? SafaKey,
    string AccountType,
    string Stage,
    string? LeadSource,
    string LeadStatus,
    decimal? ExpectedRevenue,
    bool IsActive,
    Guid? AssignedToUserId,
    string? AssignedToName,
    DateTime CreatedAt);

public record CreateCompanyRequest(
    string ArabicName,
    string EnglishName,
    string Country,
    string Phone,
    string Email,
    string? Website,
    int? SafaKey,
    string AccountType,
    string Stage,
    string? LeadSource,
    string LeadStatus,
    decimal? ExpectedRevenue,
    Guid? AssignedToUserId);

public record UpdateCompanyRequest(
    string ArabicName,
    string EnglishName,
    string Country,
    string Phone,
    string Email,
    string? Website,
    int? SafaKey,
    string AccountType,
    string Stage,
    string? LeadSource,
    string LeadStatus,
    decimal? ExpectedRevenue,
    bool IsActive);

public record AssignCompanyRequest(Guid AssignedToUserId);

// --- Queries ---
public record GetCompaniesQuery(
    string? Name,
    string? Country,
    int? SafaKey,
    string? Email,
    string? Phone,
    string? AccountType,
    string? Stage,
    string? LeadStatus,
    Guid? AssignedTo,
    Guid CurrentUserId,
    bool IsAdmin,
    int Page = 1,
    int Size = 20) : IRequest<PagedResult<CompanyDto>>;

public record GetCompanyByIdQuery(Guid Id, Guid CurrentUserId, bool IsAdmin)
    : IRequest<CompanyDto>;

// --- Commands ---
public record CreateCompanyCommand(CreateCompanyRequest Request, Guid CreatedByUserId, bool IsAdmin)
    : IRequest<CompanyDto>;

public record UpdateCompanyCommand(Guid Id, UpdateCompanyRequest Request, Guid CurrentUserId, bool IsAdmin)
    : IRequest<CompanyDto>;

public record DeleteCompanyCommand(Guid Id, Guid CurrentUserId, bool IsAdmin)
    : IRequest;

public record AssignCompanyCommand(Guid CompanyId, Guid AssignedToUserId)
    : IRequest;
