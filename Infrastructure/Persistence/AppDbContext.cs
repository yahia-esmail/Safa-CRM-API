using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

using Domain.Common;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly Guid? _tenantId;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? userService = null) : base(options)
    {
        _tenantId = userService?.TenantId;
    }
    public DbSet<SystemUser> Users => Set<SystemUser>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyContact> CompanyContacts => Set<CompanyContact>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<TechSolution> TechSolutions => Set<TechSolution>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<ImportLog> ImportLogs => Set<ImportLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // SAAS Multi-Tenancy
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<UserSmtpSetting> UserSmtpSettings => Set<UserSmtpSetting>();

    // B-1 — Notifications
    public DbSet<Notification> Notifications => Set<Notification>();

    // D-5 — Company Tags
    public DbSet<CompanyTag> CompanyTags => Set<CompanyTag>();
    public DbSet<CompanyTagAssignment> CompanyTagAssignments => Set<CompanyTagAssignment>();

    // D-6 — Company Notes
    public DbSet<CompanyNote> CompanyNotes => Set<CompanyNote>();

    // E-2 — Stage History
    public DbSet<StageHistory> StageHistories => Set<StageHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SystemUser
        modelBuilder.Entity<SystemUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.Role).HasConversion<string>();

            e.HasMany(x => x.RefreshTokens)
             .WithOne(rt => rt.User)
             .HasForeignKey(rt => rt.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Company
        modelBuilder.Entity<Company>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ArabicName).HasMaxLength(300).IsRequired();
            e.Property(x => x.EnglishName).HasMaxLength(300).IsRequired();
            e.Property(x => x.Country).HasMaxLength(100).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Website).HasMaxLength(500);
            e.Property(x => x.AccountType).HasMaxLength(20);
            e.Property(x => x.LeadSource).HasMaxLength(100);
            e.Property(x => x.LeadStatus).HasMaxLength(20);
            e.Property(x => x.ExpectedRevenue).HasPrecision(18, 2);
            e.Property(x => x.Stage).HasConversion<string>();

            e.HasOne(x => x.AssignedTo)
             .WithMany(u => u.AssignedCompanies)
             .HasForeignKey(x => x.AssignedToUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // CompanyContact
        modelBuilder.Entity<CompanyContact>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.JobTitle).HasMaxLength(200);

            e.HasOne(x => x.Company)
             .WithMany(c => c.Contacts)
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Activity
        modelBuilder.Entity<Activity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Note).HasMaxLength(2000);
            e.Property(x => x.Type).HasConversion<string>();

            e.HasOne(x => x.Company)
             .WithMany(c => c.Activities)
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.CreatedBy)
             .WithMany()
             .HasForeignKey(x => x.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // TechSolution
        modelBuilder.Entity<TechSolution>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
        });

        // SalesOrder
        modelBuilder.Entity<SalesOrder>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.InvoiceNumber).IsUnique();
            e.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.OrderReference).HasMaxLength(200);
            e.Property(x => x.SaleOrderType).HasMaxLength(20);
            e.Property(x => x.PaymentMethod).HasMaxLength(20);
            e.Property(x => x.OriginalCurrency).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.OriginalAmount).HasPrecision(18, 4);
            e.Property(x => x.UsdRateAtTime).HasPrecision(18, 8);
            e.Property(x => x.UsdAmount).HasPrecision(18, 4);
            e.Property(x => x.Attachment).HasMaxLength(500);

            e.HasOne(x => x.Company)
             .WithMany(c => c.SalesOrders)
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CreatedBy)
             .WithMany(u => u.SalesOrders)
             .HasForeignKey(x => x.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // SalesOrderItem
        modelBuilder.Entity<SalesOrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Price).HasPrecision(18, 4);
            e.Property(x => x.Note).HasMaxLength(500);
            // DateOnly stored as date (no time) in SQL Server
            e.Property(x => x.StartDate).HasColumnType("date");
            e.Property(x => x.EndDate).HasColumnType("date");

            e.HasOne(x => x.SalesOrder)
             .WithMany(o => o.Items)
             .HasForeignKey(x => x.SalesOrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Solution)
             .WithMany(s => s.OrderItems)
             .HasForeignKey(x => x.SolutionId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Invoice
        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.InvoiceNumber).IsUnique();
            e.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.PdfUrl).HasMaxLength(500);

            e.HasOne(x => x.SalesOrder)
             .WithOne(o => o.Invoice)
             .HasForeignKey<Invoice>(x => x.SalesOrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ExchangeRate — apply precision(18,8) to ALL decimal properties via reflection
        // This covers all 160+ currency columns without listing each one individually
        modelBuilder.Entity<ExchangeRate>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Date).IsUnique();
        });

        // Set decimal precision globally for all ExchangeRate decimal properties
        var exchangeRateEntity = modelBuilder.Entity<ExchangeRate>().Metadata;
        foreach (var property in exchangeRateEntity.GetProperties()
            .Where(p => p.ClrType == typeof(decimal)))
        {
            property.SetPrecision(18);
            property.SetScale(8);
        }

        // ImportLog
        modelBuilder.Entity<ImportLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(300);
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(50);

            e.HasOne(x => x.UploadedBy)
             .WithMany(u => u.ImportLogs)
             .HasForeignKey(x => x.UploadedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Token).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.CreatedByIp).HasMaxLength(50);
            e.Property(x => x.RevokedByIp).HasMaxLength(50);
            e.Property(x => x.ReplacedByToken).HasMaxLength(500);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityName).HasMaxLength(200).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.Property(x => x.UserName).HasMaxLength(200);
            e.Property(x => x.IpAddress).HasMaxLength(50);
        });

        // ── SAAS Multi-Tenancy Entities ─────────────────────────────
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Industry).HasMaxLength(100);

            e.HasOne(x => x.SubscriptionPlan)
             .WithMany(p => p.Tenants)
             .HasForeignKey(x => x.SubscriptionPlanId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SubscriptionPlan>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<UserSmtpSetting>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.Host).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Password).HasMaxLength(500);
            e.Property(x => x.Encryption).HasMaxLength(50);

            e.HasOne(x => x.User)
             .WithOne(u => u.SmtpSetting)
             .HasForeignKey<UserSmtpSetting>(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // B-1 — Notification
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Body).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Type).HasConversion<string>();
            e.Property(x => x.EntityType).HasMaxLength(100);
            e.Property(x => x.EntityId).HasMaxLength(100);

            e.HasOne(x => x.User)
             .WithMany(u => u.Notifications)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // D-5 — CompanyTag
        modelBuilder.Entity<CompanyTag>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Color).HasMaxLength(20);

            e.HasOne(x => x.CreatedBy)
             .WithMany()
             .HasForeignKey(x => x.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // D-5 — CompanyTagAssignment (composite PK)
        modelBuilder.Entity<CompanyTagAssignment>(e =>
        {
            e.HasKey(x => new { x.CompanyId, x.TagId });

            e.HasOne(x => x.Company)
             .WithMany(c => c.TagAssignments)
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Tag)
             .WithMany(t => t.Assignments)
             .HasForeignKey(x => x.TagId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // D-6 — CompanyNote
        modelBuilder.Entity<CompanyNote>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Content).HasMaxLength(4000).IsRequired();

            e.HasOne(x => x.Company)
             .WithMany(c => c.Notes)
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.CreatedBy)
             .WithMany()
             .HasForeignKey(x => x.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // E-2 — StageHistory
        modelBuilder.Entity<StageHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FromStage).HasMaxLength(100).IsRequired();
            e.Property(x => x.ToStage).HasMaxLength(100).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(500);

            e.HasOne(x => x.Company)
             .WithMany(c => c.StageHistories)
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.ChangedBy)
             .WithMany()
             .HasForeignKey(x => x.ChangedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SAAS Explicit Tenant Relations (Prevent Cascade Cycles) ───
        modelBuilder.Entity<Company>()
            .HasOne(x => x.Tenant)
            .WithMany(t => t.Companies)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompanyContact>()
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Activity>()
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SalesOrder>()
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportLog>()
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SystemUser>()
            .HasOne(x => x.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // E-2 — StageHistory tenant relation
        modelBuilder.Entity<StageHistory>()
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // D-6 — CompanyNote tenant relation
        modelBuilder.Entity<CompanyNote>()
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Global Query Filters for IMustHaveTenant ─────────────────
        modelBuilder.Entity<Company>().HasQueryFilter(e =>
            (!_tenantId.HasValue || e.TenantId == _tenantId) && e.IsActive); // I-5: soft-delete filter
        modelBuilder.Entity<CompanyContact>().HasQueryFilter(e => !_tenantId.HasValue || e.TenantId == _tenantId);
        modelBuilder.Entity<Activity>().HasQueryFilter(e => !_tenantId.HasValue || e.TenantId == _tenantId);
        modelBuilder.Entity<SalesOrder>().HasQueryFilter(e => !_tenantId.HasValue || e.TenantId == _tenantId);
        modelBuilder.Entity<ImportLog>().HasQueryFilter(e => !_tenantId.HasValue || e.TenantId == _tenantId);
        modelBuilder.Entity<SystemUser>().HasQueryFilter(e => !_tenantId.HasValue || e.TenantId == _tenantId);
        modelBuilder.Entity<StageHistory>().HasQueryFilter(e => !_tenantId.HasValue || e.TenantId == _tenantId);
        modelBuilder.Entity<CompanyNote>().HasQueryFilter(e => !_tenantId.HasValue || e.TenantId == _tenantId);
    }
}
