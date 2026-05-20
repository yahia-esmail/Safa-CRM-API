using Application.Common;
using Application.Common.Interfaces;
using Application.Features.Companies;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Companies.Handlers;

public class GetCompaniesHandler(ICompanyRepository repo)
    : IRequestHandler<GetCompaniesQuery, PagedResult<CompanyDto>>
{
    public async Task<PagedResult<CompanyDto>> Handle(GetCompaniesQuery q, CancellationToken ct)
    {
        var (items, total) = await repo.SearchAsync(
            q.Name, q.Country, q.SafaKey, q.Email, q.Phone,
            q.AccountType, q.Stage, q.LeadStatus, q.AssignedTo,
            q.CurrentUserId, q.IsAdmin, q.Page, q.Size, q.TagId);

            return new PagedResult<CompanyDto>(items.Select(ToDto), total, q.Page, q.Size);
    }

    internal static CompanyDto ToDto(Company c) => new(
        c.Id, c.ArabicName, c.EnglishName, c.Country, c.Phone, c.Email,
        c.Website, c.SafaKey, c.AccountType, c.Stage.ToString(),
        c.LeadSource, c.LeadStatus, c.ExpectedRevenue, c.IsActive,
        c.AssignedToUserId, c.AssignedTo?.Name, c.CreatedAt,
        c.ContractAttachment, c.ApplicationForm,
        c.TagAssignments.Select(ta => new Application.Features.Tags.TagDto(
            ta.Tag.Id, ta.Tag.Name, ta.Tag.Color, ta.Tag.CreatedByUserId, ta.Tag.CreatedBy?.Name ?? "")));
}

public class GetCompanyByIdHandler(IAppDbContext context)
    : IRequestHandler<GetCompanyByIdQuery, CompanyDto>
{
    public async Task<CompanyDto> Handle(GetCompanyByIdQuery q, CancellationToken ct)
    {
        var company = await context.Companies
            .Include(c => c.AssignedTo)
            .Include(c => c.TagAssignments).ThenInclude(ta => ta.Tag)
            .FirstOrDefaultAsync(c => c.Id == q.Id, ct)
            ?? throw new KeyNotFoundException($"Company {q.Id} not found.");

        if (!q.IsAdmin && company.AssignedToUserId != q.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        return GetCompaniesHandler.ToDto(company);
    }
}

public class CreateCompanyHandler(ICompanyRepository repo, IAppDbContext context)
    : IRequestHandler<CreateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(CreateCompanyCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        var phone = PhoneHelper.ToE164(r.Phone) ?? r.Phone;

        if (!Enum.TryParse<Domain.Enums.Stage>(r.Stage, true, out var stage))
            throw new ArgumentException($"Invalid stage: {r.Stage}");

        // Uniqueness validations
        if (r.SafaKey.HasValue && r.SafaKey.Value > 0 && await context.Companies.AnyAsync(c => c.SafaKey == r.SafaKey, ct))
            throw new ArgumentException("SafaKey already exists / رقم السجل (SafaKey) موجود مسبقاً");
        if (!string.IsNullOrWhiteSpace(r.Email) && await context.Companies.AnyAsync(c => c.Email == r.Email, ct))
            throw new ArgumentException("Email already exists / البريد الإلكتروني موجود مسبقاً");
        if (!string.IsNullOrWhiteSpace(phone) && await context.Companies.AnyAsync(c => c.Phone == phone, ct))
            throw new ArgumentException("Mobile/Phone already exists / رقم الجوال أو الهاتف موجود مسبقاً");

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
            AssignedToUserId = r.AssignedToUserId,
            ContractAttachment = r.ContractAttachment,
            ApplicationForm = r.ApplicationForm
        };

        await repo.AddAsync(company);
        await repo.SaveChangesAsync();

        return new CompanyDto(
            company.Id, company.ArabicName, company.EnglishName, company.Country,
            company.Phone, company.Email, company.Website, company.SafaKey,
            company.AccountType, company.Stage.ToString(), company.LeadSource,
            company.LeadStatus, company.ExpectedRevenue, company.IsActive,
            company.AssignedToUserId, null, company.CreatedAt,
            company.ContractAttachment, company.ApplicationForm,
            System.Linq.Enumerable.Empty<Application.Features.Tags.TagDto>());
    }
}

public class UpdateCompanyHandler(ICompanyRepository repo, IAppDbContext context, INotificationService notificationService)
    : IRequestHandler<UpdateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(UpdateCompanyCommand cmd, CancellationToken ct)
    {
        var company = await context.Companies
            .Include(c => c.AssignedTo)
            .Include(c => c.TagAssignments).ThenInclude(ta => ta.Tag)
            .FirstOrDefaultAsync(c => c.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException($"Company {cmd.Id} not found.");

        if (!cmd.IsAdmin && company.AssignedToUserId != cmd.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        var r = cmd.Request;
        var phone = PhoneHelper.ToE164(r.Phone) ?? r.Phone;

        if (!Enum.TryParse<Domain.Enums.Stage>(r.Stage, true, out var stage))
            throw new ArgumentException($"Invalid stage: {r.Stage}");

        // Uniqueness validations
        if (r.SafaKey.HasValue && r.SafaKey.Value > 0 && r.SafaKey != company.SafaKey && await context.Companies.AnyAsync(c => c.SafaKey == r.SafaKey, ct))
            throw new ArgumentException("SafaKey already exists / رقم السجل (SafaKey) موجود مسبقاً");
        if (!string.IsNullOrWhiteSpace(r.Email) && r.Email != company.Email && await context.Companies.AnyAsync(c => c.Email == r.Email, ct))
            throw new ArgumentException("Email already exists / البريد الإلكتروني موجود مسبقاً");
        if (!string.IsNullOrWhiteSpace(phone) && phone != company.Phone && await context.Companies.AnyAsync(c => c.Phone == phone, ct))
            throw new ArgumentException("Mobile/Phone already exists / رقم الجوال أو الهاتف موجود مسبقاً");

        // Track Stage Transition (E-2)
        var oldStage = company.Stage;
        bool stageChanged = oldStage != stage;

        // Auto Activity Log (E-3)
        if (!string.IsNullOrWhiteSpace(r.ContractAttachment) && r.ContractAttachment != company.ContractAttachment)
        {
            var activity = new Activity
            {
                CompanyId = company.Id,
                CreatedByUserId = cmd.CurrentUserId,
                Type = Domain.Enums.ActivityType.Contract,
                Note = $"System: Contract uploaded - {System.IO.Path.GetFileName(r.ContractAttachment)}",
                TenantId = company.TenantId
            };
            context.Activities.Add(activity);
        }

        if (!string.IsNullOrWhiteSpace(r.ApplicationForm) && r.ApplicationForm != company.ApplicationForm)
        {
            var activity = new Activity
            {
                CompanyId = company.Id,
                CreatedByUserId = cmd.CurrentUserId,
                Type = Domain.Enums.ActivityType.Proposal,
                Note = $"System: Application form uploaded - {System.IO.Path.GetFileName(r.ApplicationForm)}",
                TenantId = company.TenantId
            };
            context.Activities.Add(activity);
        }

        company.ArabicName = r.ArabicName;
        company.EnglishName = r.EnglishName;
        company.Country = r.Country;
        company.Phone = phone;
        company.Email = r.Email;
        company.Website = r.Website;
        company.SafaKey = r.SafaKey;
        company.AccountType = r.AccountType;
        company.Stage = stage;
        company.LeadSource = r.LeadSource;
        company.LeadStatus = r.LeadStatus;
        company.ExpectedRevenue = r.ExpectedRevenue;
        company.IsActive = r.IsActive;
        company.ContractAttachment = r.ContractAttachment;
        company.ApplicationForm = r.ApplicationForm;

        repo.Update(company);
        await repo.SaveChangesAsync();

        // Stage history logging & Notification dispatching
        if (stageChanged)
        {
            var stageHistory = new StageHistory
            {
                CompanyId = company.Id,
                FromStage = oldStage.ToString(),
                ToStage = stage.ToString(),
                ChangedByUserId = cmd.CurrentUserId,
                TenantId = company.TenantId,
                ChangedAt = DateTime.UtcNow,
                Reason = "Stage transitioned via company details update."
            };
            context.StageHistories.Add(stageHistory);
            await context.SaveChangesAsync(ct);

            // Send notification to Assigned Sales Rep (B-2)
            if (company.AssignedToUserId.HasValue)
            {
                await notificationService.SendAsync(
                    company.AssignedToUserId.Value,
                    "Company Stage Changed",
                    $"The stage for company '{company.EnglishName}' has been changed from '{oldStage}' to '{stage}'.",
                    NotificationType.StageChanged,
                    "Company",
                    company.Id.ToString());
            }
        }

        var assignedToUser = company.AssignedToUserId.HasValue
            ? await context.Users.FindAsync([company.AssignedToUserId.Value], ct)
            : null;

        return new CompanyDto(
            company.Id, company.ArabicName, company.EnglishName, company.Country,
            company.Phone, company.Email, company.Website, company.SafaKey,
            company.AccountType, company.Stage.ToString(), company.LeadSource,
            company.LeadStatus, company.ExpectedRevenue, company.IsActive,
            company.AssignedToUserId, assignedToUser?.Name, company.CreatedAt,
            company.ContractAttachment, company.ApplicationForm,
            company.TagAssignments.Select(ta => new Application.Features.Tags.TagDto(
                ta.Tag.Id, ta.Tag.Name, ta.Tag.Color, ta.Tag.CreatedByUserId, ta.Tag.CreatedBy?.Name ?? "")));
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

public class AssignCompanyHandler(ICompanyRepository repo, IAppDbContext context, INotificationService notificationService)
    : IRequestHandler<AssignCompanyCommand>
{
    public async Task Handle(AssignCompanyCommand cmd, CancellationToken ct)
    {
        var company = await repo.GetByIdAsync(cmd.CompanyId)
            ?? throw new KeyNotFoundException($"Company {cmd.CompanyId} not found.");

        var oldAssignedUserId = company.AssignedToUserId;
        company.AssignedToUserId = cmd.AssignedToUserId;
        repo.Update(company);
        await repo.SaveChangesAsync();

        // Send Notification if assigned to a new rep (B-2)
        if (oldAssignedUserId != cmd.AssignedToUserId)
        {
            await notificationService.SendAsync(
                cmd.AssignedToUserId,
                "Company Assigned",
                $"You have been assigned as the Sales Rep for company '{company.EnglishName}'.",
                NotificationType.CompanyAssigned,
                "Company",
                company.Id.ToString());
        }
    }
}

public class GetCompanyStageHistoryHandler(IAppDbContext context)
    : IRequestHandler<GetCompanyStageHistoryQuery, IEnumerable<StageHistoryDto>>
{
    public async Task<IEnumerable<StageHistoryDto>> Handle(GetCompanyStageHistoryQuery q, CancellationToken ct)
    {
        // Permission check: only admin or assigned sales rep can access
        var company = await context.Companies.FindAsync([q.CompanyId], ct)
            ?? throw new KeyNotFoundException("Company not found.");

        if (!q.IsAdmin && company.AssignedToUserId != q.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        return await context.StageHistories
            .Include(sh => sh.ChangedBy)
            .Where(sh => sh.CompanyId == q.CompanyId)
            .OrderByDescending(sh => sh.ChangedAt)
            .Select(sh => new StageHistoryDto(
                sh.Id,
                sh.FromStage,
                sh.ToStage,
                sh.Reason,
                sh.ChangedByUserId,
                sh.ChangedBy.Name,
                sh.ChangedAt))
            .ToListAsync(ct);
    }
}

