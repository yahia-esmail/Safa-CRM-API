using Application.Common;
using Application.Features.Companies;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Features.Companies.Handlers;

public class GetCompaniesHandler(ICompanyRepository repo)
    : IRequestHandler<GetCompaniesQuery, PagedResult<CompanyDto>>
{
    public async Task<PagedResult<CompanyDto>> Handle(GetCompaniesQuery q, CancellationToken ct)
    {
        var (items, total) = await repo.SearchAsync(
            q.Name, q.Country, q.SafaKey, q.Email, q.Phone,
            q.AccountType, q.Stage, q.LeadStatus, q.AssignedTo,
            q.CurrentUserId, q.IsAdmin, q.Page, q.Size);

        return new PagedResult<CompanyDto>
        {
            Items = items.Select(ToDto),
            TotalCount = total,
            Page = q.Page,
            Size = q.Size
        };
    }

    private static CompanyDto ToDto(Company c) => new(
        c.Id, c.ArabicName, c.EnglishName, c.Country, c.Phone, c.Email,
        c.Website, c.SafaKey, c.AccountType, c.Stage.ToString(),
        c.LeadSource, c.LeadStatus, c.ExpectedRevenue, c.IsActive,
        c.AssignedToUserId, c.AssignedTo?.Name, c.CreatedAt);
}

public class GetCompanyByIdHandler(ICompanyRepository repo)
    : IRequestHandler<GetCompanyByIdQuery, CompanyDto>
{
    public async Task<CompanyDto> Handle(GetCompanyByIdQuery q, CancellationToken ct)
    {
        var company = await repo.GetByIdAsync(q.Id)
            ?? throw new KeyNotFoundException($"Company {q.Id} not found.");

        if (!q.IsAdmin && company.AssignedToUserId != q.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        return new CompanyDto(
            company.Id, company.ArabicName, company.EnglishName, company.Country,
            company.Phone, company.Email, company.Website, company.SafaKey,
            company.AccountType, company.Stage.ToString(), company.LeadSource,
            company.LeadStatus, company.ExpectedRevenue, company.IsActive,
            company.AssignedToUserId, company.AssignedTo?.Name, company.CreatedAt);
    }
}

public class CreateCompanyHandler(ICompanyRepository repo)
    : IRequestHandler<CreateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(CreateCompanyCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        var phone = PhoneHelper.ToE164(r.Phone) ?? r.Phone;

        if (!Enum.TryParse<Domain.Enums.Stage>(r.Stage, true, out var stage))
            throw new ArgumentException($"Invalid stage: {r.Stage}");

        var company = new Company
        {
            ArabicName = r.ArabicName,
            EnglishName = r.EnglishName,
            Country = r.Country,
            Phone = phone,
            Email = r.Email,
            Website = r.Website,
            SafaKey = r.SafaKey,
            AccountType = r.AccountType,
            Stage = stage,
            LeadSource = r.LeadSource,
            LeadStatus = r.LeadStatus,
            ExpectedRevenue = r.ExpectedRevenue,
            AssignedToUserId = r.AssignedToUserId
        };

        await repo.AddAsync(company);
        await repo.SaveChangesAsync();

        return new CompanyDto(
            company.Id, company.ArabicName, company.EnglishName, company.Country,
            company.Phone, company.Email, company.Website, company.SafaKey,
            company.AccountType, company.Stage.ToString(), company.LeadSource,
            company.LeadStatus, company.ExpectedRevenue, company.IsActive,
            company.AssignedToUserId, null, company.CreatedAt);
    }
}

public class UpdateCompanyHandler(ICompanyRepository repo)
    : IRequestHandler<UpdateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(UpdateCompanyCommand cmd, CancellationToken ct)
    {
        var company = await repo.GetByIdAsync(cmd.Id)
            ?? throw new KeyNotFoundException($"Company {cmd.Id} not found.");

        if (!cmd.IsAdmin && company.AssignedToUserId != cmd.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        var r = cmd.Request;
        if (!Enum.TryParse<Domain.Enums.Stage>(r.Stage, true, out var stage))
            throw new ArgumentException($"Invalid stage: {r.Stage}");

        company.ArabicName = r.ArabicName;
        company.EnglishName = r.EnglishName;
        company.Country = r.Country;
        company.Phone = PhoneHelper.ToE164(r.Phone) ?? r.Phone;
        company.Email = r.Email;
        company.Website = r.Website;
        company.SafaKey = r.SafaKey;
        company.AccountType = r.AccountType;
        company.Stage = stage;
        company.LeadSource = r.LeadSource;
        company.LeadStatus = r.LeadStatus;
        company.ExpectedRevenue = r.ExpectedRevenue;
        company.IsActive = r.IsActive;

        repo.Update(company);
        await repo.SaveChangesAsync();

        return new CompanyDto(
            company.Id, company.ArabicName, company.EnglishName, company.Country,
            company.Phone, company.Email, company.Website, company.SafaKey,
            company.AccountType, company.Stage.ToString(), company.LeadSource,
            company.LeadStatus, company.ExpectedRevenue, company.IsActive,
            company.AssignedToUserId, null, company.CreatedAt);
    }
}

public class DeleteCompanyHandler(ICompanyRepository repo)
    : IRequestHandler<DeleteCompanyCommand>
{
    public async Task Handle(DeleteCompanyCommand cmd, CancellationToken ct)
    {
        var company = await repo.GetByIdAsync(cmd.Id)
            ?? throw new KeyNotFoundException($"Company {cmd.Id} not found.");

        if (!cmd.IsAdmin && company.AssignedToUserId != cmd.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        company.IsActive = false;  // Soft delete
        repo.Update(company);
        await repo.SaveChangesAsync();
    }
}

public class AssignCompanyHandler(ICompanyRepository repo)
    : IRequestHandler<AssignCompanyCommand>
{
    public async Task Handle(AssignCompanyCommand cmd, CancellationToken ct)
    {
        var company = await repo.GetByIdAsync(cmd.CompanyId)
            ?? throw new KeyNotFoundException($"Company {cmd.CompanyId} not found.");

        company.AssignedToUserId = cmd.AssignedToUserId;
        repo.Update(company);
        await repo.SaveChangesAsync();
    }
}
