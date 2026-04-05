using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<SystemUser> Users => Set<SystemUser>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyContact> CompanyContacts => Set<CompanyContact>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<TechSolution> TechSolutions => Set<TechSolution>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<ImportLog> ImportLogs => Set<ImportLog>();

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
            e.Property(x => x.Currency).HasConversion<string>();
            e.Property(x => x.Note).HasMaxLength(500);

            e.HasOne(x => x.SalesOrder)
             .WithMany(o => o.Items)
             .HasForeignKey(x => x.SalesOrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Solution)
             .WithMany(s => s.OrderItems)
             .HasForeignKey(x => x.SolutionId)
             .OnDelete(DeleteBehavior.Restrict);
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
    }
}
