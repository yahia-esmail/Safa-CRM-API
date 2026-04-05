using Application.Common.Interfaces;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Rates;

// --- DTOs ---
public record ExchangeRateDto(
    Guid Id, DateTime Date,
    decimal USD, decimal EGP, decimal SAR, decimal JOD,
    decimal AED, decimal EUR, decimal GBP, decimal QAR, decimal KWD);

// --- Queries ---
public record GetTodayRateQuery : IRequest<ExchangeRateDto>;
public record GetRateHistoryQuery(DateTime From, DateTime To) : IRequest<IEnumerable<ExchangeRateDto>>;
public record RefreshRatesCommand : IRequest;

// --- Handlers ---
public class GetTodayRateHandler(IExchangeRateRepository repo)
    : IRequestHandler<GetTodayRateQuery, ExchangeRateDto>
{
    public async Task<ExchangeRateDto> Handle(GetTodayRateQuery q, CancellationToken ct)
    {
        var rate = await repo.GetTodayRateAsync() ?? await repo.GetLatestRateAsync()
            ?? throw new KeyNotFoundException("No exchange rate data found.");
        return new ExchangeRateDto(rate.Id, rate.Date, rate.USD, rate.EGP, rate.SAR, rate.JOD,
            rate.AED, rate.EUR, rate.GBP, rate.QAR, rate.KWD);
    }
}

public class GetRateHistoryHandler(IExchangeRateRepository repo)
    : IRequestHandler<GetRateHistoryQuery, IEnumerable<ExchangeRateDto>>
{
    public async Task<IEnumerable<ExchangeRateDto>> Handle(GetRateHistoryQuery q, CancellationToken ct)
    {
        var rates = await repo.GetHistoryAsync(q.From, q.To);
        return rates.Select(r => new ExchangeRateDto(r.Id, r.Date, r.USD, r.EGP, r.SAR, r.JOD,
            r.AED, r.EUR, r.GBP, r.QAR, r.KWD));
    }
}

public class RefreshRatesHandler(IExchangeRateFetcher fetcher)
    : IRequestHandler<RefreshRatesCommand>
{
    public async Task Handle(RefreshRatesCommand cmd, CancellationToken ct) =>
        await fetcher.FetchAndSaveRatesAsync();
}
