using COEPD.SalesFunnelSystem.Application.Interfaces;
using COEPD.SalesFunnelSystem.Domain.Entities;
using COEPD.SalesFunnelSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace COEPD.SalesFunnelSystem.Infrastructure.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly ApplicationDbContext _db;
    public LeadRepository(ApplicationDbContext db) => _db = db;

    public async Task<Lead> AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(cancellationToken);
        return lead;
    }

    public async Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        _db.Leads.Update(lead);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int?> GetLatestLeadIdAsync(CancellationToken cancellationToken = default) =>
        _db.Leads.OrderByDescending(x => x.CreatedAt).Select(x => (int?)x.Id).FirstOrDefaultAsync(cancellationToken);

    public Task<Lead?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Leads.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByEmailOrPhoneAsync(string email, string phone, CancellationToken cancellationToken = default) =>
        _db.Leads.AsNoTracking()
            .AnyAsync(
                x => x.Email == email || x.Phone == phone,
                cancellationToken);

    public async Task<List<Lead>> GetFilteredAsync(
        string? search,
        string? source,
        string? status,
        string? domain,
        DateTime? fromDate,
        DateTime? toDate,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Leads.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.Name, $"%{term}%") ||
                EF.Functions.Like(x.Phone, $"%{term}%") ||
                EF.Functions.Like(x.Email, $"%{term}%") ||
                EF.Functions.Like(x.Domain, $"%{term}%"));
        }
        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(x => x.Source == source);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(domain))
            query = query.Where(x => x.Domain == domain);
        if (fromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
        {
            var endDateExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < endDateExclusive);
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        if (pageNumber.HasValue && pageSize.HasValue && pageNumber.Value > 0 && pageSize.Value > 0)
        {
            var safePageSize = Math.Min(pageSize.Value, 200);
            var skip = (pageNumber.Value - 1) * safePageSize;
            query = query.Skip(skip).Take(safePageSize);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        _db.Leads.Remove(lead);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => _db.Leads.CountAsync(cancellationToken);

    public Task<int> CountTodayAsync(CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);
        return _db.Leads.CountAsync(x => x.CreatedAt >= start && x.CreatedAt < end, cancellationToken);
    }

    public Task<int> CountThisMonthAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        return _db.Leads.CountAsync(x => x.CreatedAt >= start && x.CreatedAt < end, cancellationToken);
    }

    public Task<int> CountByStatusAsync(string status, CancellationToken cancellationToken = default) =>
        _db.Leads.CountAsync(x => x.Status == status, cancellationToken);

    public async Task<Dictionary<string, int>> GetSourceBreakdownAsync(CancellationToken cancellationToken = default) =>
        await _db.Leads.AsNoTracking().GroupBy(x => x.Source).Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

    public async Task<List<(DateTime Date, int Count)>> GetLeadGrowthAsync(int days, CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow.Date.AddDays(-(days - 1));
        var grouped = await _db.Leads.AsNoTracking()
            .Where(x => x.CreatedAt >= start)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(x => new { Date = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        return Enumerable.Range(0, days)
            .Select(offset => start.AddDays(offset))
            .Select(date => (date, grouped.FirstOrDefault(x => x.Date == date)?.Count ?? 0))
            .ToList();
    }
}

public class DemoBookingRepository : IDemoBookingRepository
{
    private readonly ApplicationDbContext _db;
    public DemoBookingRepository(ApplicationDbContext db) => _db = db;

    public async Task<DemoBooking> AddAsync(DemoBooking booking, CancellationToken cancellationToken = default)
    {
        _db.DemoBookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task<bool> IsSlotAvailableAsync(string day, string slot, CancellationToken cancellationToken = default)
    {
        var slotTaken = await _db.DemoBookings.AsNoTracking()
            .Where(x => x.Day == day && x.Slot == slot)
            .Where(x => x.Status == DemoBookingStatuses.Pending || x.Status == DemoBookingStatuses.Confirmed)
            .AnyAsync(cancellationToken);

        return !slotTaken;
    }

    public Task<List<DemoBooking>> GetByLeadIdAsync(int leadId, CancellationToken cancellationToken = default) =>
        _db.DemoBookings.AsNoTracking()
            .Where(x => x.LeadId == leadId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<DemoBooking?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.DemoBookings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task UpdateAsync(DemoBooking booking, CancellationToken cancellationToken = default)
    {
        _db.DemoBookings.Update(booking);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => _db.DemoBookings.CountAsync(cancellationToken);
}

public class FunnelEventRepository : IFunnelEventRepository
{
    private readonly ApplicationDbContext _db;
    public FunnelEventRepository(ApplicationDbContext db) => _db = db;

    public async Task AddAsync(FunnelEvent funnelEvent, CancellationToken cancellationToken = default)
    {
        _db.FunnelEvents.Add(funnelEvent);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetStageCountsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.FunnelEvents.AsNoTracking()
            .GroupBy(x => x.Stage)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
    }

    public async Task<List<(DateTime Date, string Stage, int Count)>> GetStageTrendAsync(int days, CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow.Date.AddDays(-(days - 1));
        var trendRows = await _db.FunnelEvents.AsNoTracking()
            .Where(x => x.Timestamp >= start)
            .GroupBy(x => new { Date = x.Timestamp.Date, x.Stage })
            .Select(x => new { x.Key.Date, x.Key.Stage, Count = x.Count() })
            .ToListAsync(cancellationToken);

        return trendRows.Select(x => (x.Date, x.Stage, x.Count)).ToList();
    }
}

public class LeadActivityRepository : ILeadActivityRepository
{
    private readonly ApplicationDbContext _db;
    public LeadActivityRepository(ApplicationDbContext db) => _db = db;

    public async Task AddAsync(LeadActivityLog activity, CancellationToken cancellationToken = default)
    {
        _db.LeadActivityLogs.Add(activity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class LeadFollowUpJobRepository : ILeadFollowUpJobRepository
{
    private readonly ApplicationDbContext _db;

    public LeadFollowUpJobRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(LeadFollowUpJob job, CancellationToken cancellationToken = default)
    {
        _db.LeadFollowUpJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<List<LeadFollowUpJob>> GetDuePendingAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default) =>
        _db.LeadFollowUpJobs
            .Where(x => x.Status == FollowUpJobStatuses.Pending && x.DueAt <= utcNow)
            .OrderBy(x => x.DueAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task UpdateAsync(LeadFollowUpJob job, CancellationToken cancellationToken = default)
    {
        _db.LeadFollowUpJobs.Update(job);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class ChatRepository : IChatRepository
{
    private readonly ApplicationDbContext _db;
    public ChatRepository(ApplicationDbContext db) => _db = db;

    public Task<ChatSession?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default) =>
        _db.ChatSessions.FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);

    public async Task<ChatSession> AddAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task SaveMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        _db.ChatSessions.Update(session);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;
    public UserRepository(ApplicationDbContext db) => _db = db;

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _db.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

    public Task<AppUser?> GetByEmailForUpdateAsync(string email, CancellationToken cancellationToken = default) =>
        _db.AppUsers.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

    public Task<AppUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.AppUsers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<AppUser>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.AppUsers.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _db.AppUsers.AsNoTracking().AnyAsync(x => x.Email == email, cancellationToken);

    public async Task<AppUser> AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        _db.AppUsers.Update(user);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
