using Application.Common.Interfaces;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Domain.Entities;

namespace Application.Features.Rates;

// --- Queries ---
public record GetTodayRateQuery : IRequest<ExchangeRate>;
public record GetRateHistoryQuery(DateTime From, DateTime To) : IRequest<IEnumerable<ExchangeRate>>;
public record RefreshRatesCommand : IRequest;

// --- Handlers ---
public class GetTodayRateHandler(IExchangeRateRepository repo)
    : IRequestHandler<GetTodayRateQuery, ExchangeRate>
{
    public async Task<ExchangeRate> Handle(GetTodayRateQuery q, CancellationToken ct)
    {
        return await repo.GetTodayRateAsync() ?? await repo.GetLatestRateAsync()
            ?? throw new KeyNotFoundException("No exchange rate data found.");
    }
}

public class GetRateHistoryHandler(IExchangeRateRepository repo)
    : IRequestHandler<GetRateHistoryQuery, IEnumerable<ExchangeRate>>
{
    public async Task<IEnumerable<ExchangeRate>> Handle(GetRateHistoryQuery q, CancellationToken ct)
    {
        return await repo.GetHistoryAsync(q.From, q.To);
    }
}

public class RefreshRatesHandler(IExchangeRateFetcher fetcher)
    : IRequestHandler<RefreshRatesCommand>
{
    public async Task Handle(RefreshRatesCommand cmd, CancellationToken ct) =>
        await fetcher.FetchAndSaveRatesAsync();
}
