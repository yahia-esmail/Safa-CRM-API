using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces;

public interface IExchangeRateRepository : IGenericRepository<ExchangeRate>
{
    Task<ExchangeRate?> GetTodayRateAsync();
    Task<ExchangeRate?> GetLatestRateAsync();
    Task<IEnumerable<ExchangeRate>> GetHistoryAsync(DateTime from, DateTime to);

    /// <summary>
    /// Gets the exchange rate value (units per 1 USD) for a given currency from a rate snapshot.
    /// Uses reflection to map enum name to the corresponding property.
    /// </summary>
    static decimal GetRateForCurrency(ExchangeRate rate, Currency currency)
    {
        if (currency == Currency.USD) return 1m;
        var prop = typeof(ExchangeRate).GetProperty(currency.ToString());
        if (prop is null) throw new InvalidOperationException($"No rate property found for currency: {currency}");
        return (decimal)(prop.GetValue(rate) ?? 0m);
    }

    /// <summary>
    /// Calculates the USD amount from an original amount in a given currency.
    /// </summary>
    static decimal ConvertToUsd(ExchangeRate rate, Currency currency, decimal amount)
    {
        var rateValue = GetRateForCurrency(rate, currency);
        if (rateValue == 0) throw new InvalidOperationException($"Exchange rate for {currency} is zero.");
        return Math.Round(amount / rateValue, 4);
    }
}
