using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace COEPD.SalesFunnelSystem.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private const string DashboardStatsCacheKey = "dashboard-stats";
    private const string LeadStatsCacheKey = "lead-stats";
    private const string LeadGrowthCacheKey = "lead-growth";
    private const string LeadAnalyticsCacheKey = "lead-analytics";
    private readonly ILeadRepository _leadRepository;
    private readonly IDemoBookingRepository _demoBookingRepository;
    private readonly IMemoryCache _memoryCache;

    public AnalyticsService(ILeadRepository leadRepository, IDemoBookingRepository demoBookingRepository, IMemoryCache memoryCache)
    {
        _leadRepository = leadRepository;
        _demoBookingRepository = demoBookingRepository;
        _memoryCache = memoryCache;
    }

    public async Task<DashboardStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        return await _memoryCache.GetOrCreateAsync(DashboardStatsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);

            var totalLeads = await _leadRepository.CountAsync(cancellationToken);
            var todayLeads = await _leadRepository.CountTodayAsync(cancellationToken);
            var conversionCount = await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.Converted, cancellationToken);
            var thisMonthLeads = await _leadRepository.CountThisMonthAsync(cancellationToken);
            var totalBookings = await _demoBookingRepository.CountAsync(cancellationToken);
            var sourceBreakdown = await _leadRepository.GetSourceBreakdownAsync(cancellationToken);
            var trendData = await _leadRepository.GetLeadGrowthAsync(14, cancellationToken);
            var previousWeek = trendData.Take(7).Sum(x => x.Count);
            var currentWeek = trendData.Skip(7).Take(7).Sum(x => x.Count);

            return new DashboardStatsResponse
            {
                TotalLeads = totalLeads,
                TodayLeads = todayLeads,
                ConversionCount = conversionCount,
                ThisMonthLeads = thisMonthLeads,
                TotalBookings = totalBookings,
                WeeklyGrowthPercentage = previousWeek == 0 ? 100 : Math.Round(((decimal)(currentWeek - previousWeek) / previousWeek) * 100, 2),
                SourceBreakdown = sourceBreakdown
            };
        }) ?? new DashboardStatsResponse();
    }

    public async Task<LeadStatsResponse> GetLeadStatsAsync(CancellationToken cancellationToken = default) =>
        await _memoryCache.GetOrCreateAsync(LeadStatsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);

            var totalLeads = await _leadRepository.CountAsync(cancellationToken);
            var todayLeads = await _leadRepository.CountTodayAsync(cancellationToken);
            var thisMonthLeads = await _leadRepository.CountThisMonthAsync(cancellationToken);
            var demoBookedLeads = await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.DemoBooked, cancellationToken)
                + await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.Demo, cancellationToken)
                + await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.BookedLegacy, cancellationToken);
            var convertedLeads = await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.Converted, cancellationToken);
            var newLeads = await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.New, cancellationToken);
            var contactedLeads = await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.Contacted, cancellationToken);

            return new LeadStatsResponse
            {
                TotalLeads = totalLeads,
                TodayLeads = todayLeads,
                ThisMonthLeads = thisMonthLeads,
                DemoBookedLeads = demoBookedLeads,
                ConvertedLeads = convertedLeads,
                ConversionRate = totalLeads == 0 ? 0 : Math.Round((decimal)convertedLeads / totalLeads * 100m, 2),
                StatusBreakdown = new Dictionary<string, int>
                {
                    ["New"] = newLeads,
                    ["Contacted"] = contactedLeads,
                    ["DemoBooked"] = demoBookedLeads,
                    ["Converted"] = convertedLeads
                }
            };
        }) ?? new LeadStatsResponse();

    public async Task<List<LeadGrowthPoint>> GetLeadGrowthAsync(int days = 7, CancellationToken cancellationToken = default) =>
        await _memoryCache.GetOrCreateAsync($"{LeadGrowthCacheKey}:{Math.Clamp(days, 1, 90)}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
            var safeDays = Math.Clamp(days, 1, 90);

            return (await _leadRepository.GetLeadGrowthAsync(safeDays, cancellationToken))
                .Select(x => new LeadGrowthPoint { Label = x.Date.ToString("dd MMM"), Count = x.Count })
                .ToList();
        }) ?? [];

    public async Task<LeadAnalyticsResponse> GetLeadAnalyticsAsync(CancellationToken cancellationToken = default) =>
        await _memoryCache.GetOrCreateAsync(LeadAnalyticsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);

            var totalLeads = await _leadRepository.CountAsync(cancellationToken);
            var newLeads = await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.New, cancellationToken);
            var contactedLeads = await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.Contacted, cancellationToken);
            var demoLeads = await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.DemoBooked, cancellationToken)
                + await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.Demo, cancellationToken)
                + await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.BookedLegacy, cancellationToken);
            var convertedLeads = await _leadRepository.CountByStatusAsync(Domain.Entities.LeadStatuses.Converted, cancellationToken);
            var sourceBreakdown = await _leadRepository.GetSourceBreakdownAsync(cancellationToken);

            var statusBreakdown = new Dictionary<string, int>
            {
                ["New"] = newLeads,
                ["Contacted"] = contactedLeads,
                ["Demo"] = demoLeads,
                ["Converted"] = convertedLeads
            };

            return new LeadAnalyticsResponse
            {
                TotalLeads = totalLeads,
                NewLeads = newLeads,
                ContactedLeads = contactedLeads,
                DemoLeads = demoLeads,
                ConvertedLeads = convertedLeads,
                ConversionRate = totalLeads == 0 ? 0 : Math.Round((decimal)convertedLeads / totalLeads * 100m, 2),
                StatusBreakdown = statusBreakdown,
                SourceBreakdown = sourceBreakdown
            };
        }) ?? new LeadAnalyticsResponse();

    public void InvalidateDashboardCache()
    {
        _memoryCache.Remove(DashboardStatsCacheKey);
        _memoryCache.Remove(LeadStatsCacheKey);
        _memoryCache.Remove(LeadAnalyticsCacheKey);
    }
}
