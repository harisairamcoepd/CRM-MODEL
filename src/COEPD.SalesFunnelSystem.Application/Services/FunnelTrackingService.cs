using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using COEPD.SalesFunnelSystem.Domain.Entities;

namespace COEPD.SalesFunnelSystem.Application.Services;

public class FunnelTrackingService : IFunnelTrackingService
{
    private static readonly HashSet<string> ValidStages = new(StringComparer.OrdinalIgnoreCase)
    {
        FunnelEventStages.Awareness,
        FunnelEventStages.Interest,
        FunnelEventStages.Desire,
        FunnelEventStages.Action
    };

    private readonly IFunnelEventRepository _funnelEventRepository;

    public FunnelTrackingService(IFunnelEventRepository funnelEventRepository)
    {
        _funnelEventRepository = funnelEventRepository;
    }

    public async Task TrackAsync(int leadId, string stage, CancellationToken cancellationToken = default)
    {
        if (leadId <= 0)
            throw new ArgumentException("LeadId must be greater than zero.");

        if (!ValidStages.Contains(stage))
            throw new ArgumentException("Invalid funnel stage.");

        var normalized = stage switch
        {
            var s when s.Equals(FunnelEventStages.Awareness, StringComparison.OrdinalIgnoreCase) => FunnelEventStages.Awareness,
            var s when s.Equals(FunnelEventStages.Interest, StringComparison.OrdinalIgnoreCase) => FunnelEventStages.Interest,
            var s when s.Equals(FunnelEventStages.Desire, StringComparison.OrdinalIgnoreCase) => FunnelEventStages.Desire,
            _ => FunnelEventStages.Action
        };

        await _funnelEventRepository.AddAsync(new FunnelEvent
        {
            LeadId = leadId,
            Stage = normalized,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task<FunnelStageCountResponse> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _funnelEventRepository.GetStageCountsAsync(cancellationToken);
        return new FunnelStageCountResponse
        {
            Awareness = counts.GetValueOrDefault(FunnelEventStages.Awareness, 0),
            Interest = counts.GetValueOrDefault(FunnelEventStages.Interest, 0),
            Desire = counts.GetValueOrDefault(FunnelEventStages.Desire, 0),
            Action = counts.GetValueOrDefault(FunnelEventStages.Action, 0)
        };
    }

    public async Task<FunnelAnalyticsResponse> GetAnalyticsAsync(int days, CancellationToken cancellationToken = default)
    {
        if (days <= 0)
        {
            throw new ArgumentException("Days must be greater than zero.");
        }

        var counts = await GetCountsAsync(cancellationToken);
        var trendRows = await _funnelEventRepository.GetStageTrendAsync(days, cancellationToken);

        var start = DateTime.UtcNow.Date.AddDays(-(days - 1));
        var trend = Enumerable.Range(0, days)
            .Select(offset => start.AddDays(offset))
            .Select(date => new FunnelTrendPoint
            {
                Date = date.ToString("yyyy-MM-dd"),
                Awareness = trendRows.Where(x => x.Date == date && x.Stage == FunnelEventStages.Awareness).Sum(x => x.Count),
                Interest = trendRows.Where(x => x.Date == date && x.Stage == FunnelEventStages.Interest).Sum(x => x.Count),
                Desire = trendRows.Where(x => x.Date == date && x.Stage == FunnelEventStages.Desire).Sum(x => x.Count),
                Action = trendRows.Where(x => x.Date == date && x.Stage == FunnelEventStages.Action).Sum(x => x.Count)
            })
            .ToList();

        return new FunnelAnalyticsResponse
        {
            StageCounts = counts,
            AwarenessToInterestRate = CalculateRate(counts.Interest, counts.Awareness),
            InterestToDesireRate = CalculateRate(counts.Desire, counts.Interest),
            DesireToActionRate = CalculateRate(counts.Action, counts.Desire),
            OverallConversionRate = CalculateRate(counts.Action, counts.Awareness),
            Trend = trend
        };
    }

    private static decimal CalculateRate(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return 0;
        }

        return Math.Round((decimal)numerator / denominator * 100m, 2);
    }
}
