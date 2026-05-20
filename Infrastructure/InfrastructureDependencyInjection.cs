using Application.Common.Interfaces;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Repositories;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Email;
using Infrastructure.Services.ExchangeRate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Interceptors
        services.AddSingleton<SanitizationInterceptor>();
        services.AddScoped<AuditInterceptor>();

        // EF Core — register both concrete and Application interface
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .AddInterceptors(
                       sp.GetRequiredService<SanitizationInterceptor>(),
                       sp.GetRequiredService<AuditInterceptor>());
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Domain Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();

        // Infrastructure Services (concrete)
        services.AddScoped<JwtService>();
        services.AddScoped<EmailService>();
        services.AddScoped<ExchangeRateService>();
        services.AddScoped<Infrastructure.Services.Jobs.RenewalReminderJob>();
        services.AddScoped<Infrastructure.Services.Jobs.ActivityOverdueJob>();

        // Application Interface → Infrastructure Adapter mappings
        services.AddScoped<IJwtService, JwtServiceAdapter>();
        services.AddScoped<IEmailService, EmailServiceAdapter>();
        services.AddScoped<IExchangeRateFetcher, ExchangeRateFetcherAdapter>();
        services.AddScoped<IInvoiceService, Infrastructure.Services.Pdf.PdfInvoiceService>();
        services.AddScoped<IFileUploadService, Infrastructure.Services.Files.FileUploadService>();
        services.AddScoped<IExcelImportService, Infrastructure.Services.Excel.ExcelImportService>();
        services.AddScoped<INotificationService, Infrastructure.Services.Notifications.NotificationService>();
        services.AddSingleton<IEncryptionService, Infrastructure.Services.Security.EncryptionService>();
        services.AddScoped<IExportService, Infrastructure.Services.Excel.ExportService>();


        // AI — Gemini
        services.Configure<Infrastructure.AI.GeminiOptions>(
            configuration.GetSection("AI"));
        services.AddHttpClient<IGeminiService, Infrastructure.AI.GeminiService>();

        // Caching (for import preview sessions)
        services.AddMemoryCache();

        // HttpClient for exchange rate API
        services.AddHttpClient();

        return services;
    }
}
