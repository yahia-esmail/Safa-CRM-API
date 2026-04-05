namespace Application.Common.Interfaces;

/// <summary>Application-level exchange rate fetching abstraction</summary>
public interface IExchangeRateFetcher
{
    Task FetchAndSaveRatesAsync();
}
