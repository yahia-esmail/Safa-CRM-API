using Application.Common.Interfaces;
using Domain.Interfaces;
using Infrastructure.Persistence;
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
        // EF Core — register both concrete and Application interface
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
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

        // Application Interface → Infrastructure Adapter mappings
        services.AddScoped<IJwtService, JwtServiceAdapter>();
        services.AddScoped<IEmailService, EmailServiceAdapter>();
        services.AddScoped<IExchangeRateFetcher, ExchangeRateFetcherAdapter>();

        // HttpClient for exchange rate API
        services.AddHttpClient();

        return services;
    }
}
