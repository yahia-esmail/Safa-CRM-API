using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Reflection;

namespace Infrastructure.Services.ExchangeRate;

public class ExchangeRateService(
    IExchangeRateRepository rateRepo,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ExchangeRateService> logger)
{
    public async Task FetchAndSaveRatesAsync()
    {
        var today = DateTime.UtcNow.Date;
        var existing = await rateRepo.GetTodayRateAsync();
        if (existing is not null)
        {
            logger.LogInformation("Exchange rates for {Date} already exist. Skipping.", today);
            return;
        }

        try
        {
            var apiKey = configuration["ExchangeRate:ApiKey"];
            var client = httpClientFactory.CreateClient();
            var url = $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/USD";

            var response = await client.GetFromJsonAsync<ExchangeRateApiResponse>(url);
            if (response is null || response.Result != "success")
            {
                logger.LogError("Failed to fetch exchange rates from API.");
                return;
            }

            var rate = new Domain.Entities.ExchangeRate { Date = today };

            // Map rates via reflection — each property name matches the currency code
            var rates = response.Conversion_Rates;
            var props = typeof(Domain.Entities.ExchangeRate)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(decimal) && p.Name != nameof(Domain.Entities.ExchangeRate.Id));

            foreach (var prop in props)
            {
                if (rates.TryGetValue(prop.Name, out var value))
                    prop.SetValue(rate, value);
            }

            await rateRepo.AddAsync(rate);
            await rateRepo.SaveChangesAsync();
            logger.LogInformation("Exchange rates saved for {Date}.", today);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching exchange rates.");
        }
    }
}
