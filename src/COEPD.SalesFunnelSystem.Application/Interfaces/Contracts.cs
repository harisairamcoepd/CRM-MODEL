using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Domain.Entities;

namespace COEPD.SalesFunnelSystem.Application.Interfaces;

public interface ILeadRepository
{
    Task<Lead> AddAsync(Lead lead, CancellationToken cancellationToken = default);
    Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default);
    Task<int?> GetLatestLeadIdAsync(CancellationToken cancellationToken = default);
    Task<Lead?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailOrPhoneAsync(string email, string phone, CancellationToken cancellationToken = default);
    Task<List<Lead>> GetFilteredAsync(
        string? search,
        string? source,
        string? status,
        string? domain,
        DateTime? fromDate,
        DateTime? toDate,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken = default);
    Task<List<Lead>> GetTodayAsync(CancellationToken cancellationToken = default);
    Task<List<Lead>> GetAssignedToStaffAsync(int staffId, CancellationToken cancellationToken = default);
    Task<int> CountAssignedToStaffAsync(int staffId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Lead lead, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountTodayAsync(CancellationToken cancellationToken = default);
    Task<int> CountThisMonthAsync(CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetSourceBreakdownAsync(CancellationToken cancellationToken = default);
    Task<List<(DateTime Date, int Count)>> GetLeadGrowthAsync(int days, CancellationToken cancellationToken = default);
}

public interface IDemoBookingRepository
{
    Task<DemoBooking> AddAsync(DemoBooking booking, CancellationToken cancellationToken = default);
    Task<bool> IsSlotAvailableAsync(string day, string slot, CancellationToken cancellationToken = default);
    Task<List<DemoBooking>> GetByLeadIdAsync(int leadId, CancellationToken cancellationToken = default);
    Task<DemoBooking?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateAsync(DemoBooking booking, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

public interface ILeadActivityRepository
{
    Task AddAsync(LeadActivityLog activity, CancellationToken cancellationToken = default);
}

public interface ILeadFollowUpJobRepository
{
    Task AddAsync(LeadFollowUpJob job, CancellationToken cancellationToken = default);
    Task<List<LeadFollowUpJob>> GetDuePendingAsync(DateTime utcNow, int take, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeadFollowUpJob job, CancellationToken cancellationToken = default);
}

public interface IChatRepository
{
    Task<ChatSession?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<ChatSession> AddAsync(ChatSession session, CancellationToken cancellationToken = default);
    Task SaveMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChatSession session, CancellationToken cancellationToken = default);
}

public interface IFunnelEventRepository
{
    Task AddAsync(FunnelEvent funnelEvent, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetStageCountsAsync(CancellationToken cancellationToken = default);
    Task<List<(DateTime Date, string Stage, int Count)>> GetStageTrendAsync(int days, CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByEmailForUpdateAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<AppUser>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser> AddAsync(AppUser user, CancellationToken cancellationToken = default);
    Task UpdateAsync(AppUser user, CancellationToken cancellationToken = default);
}

public interface ILeadService
{
    Task<LeadResponse> CreateAsync(CreateLeadRequest request, CancellationToken cancellationToken = default);
    Task<List<LeadResponse>> GetAllAsync(LeadFilterRequest filter, CancellationToken cancellationToken = default);
    Task<List<LeadResponse>> GetTodayAsync(CancellationToken cancellationToken = default);
    Task<List<LeadResponse>> GetAssignedAsync(int staffId, CancellationToken cancellationToken = default);
    Task<LeadResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UpdateLeadStatusResponse> UpdateStatusAsync(int id, UpdateLeadStatusRequest request, CancellationToken cancellationToken = default);
    Task<LeadAssignmentResponse> AssignLeadAsync(int leadId, int staffId, CancellationToken cancellationToken = default);
    Task<PipelineBoardResponse> GetPipelineAsync(LeadFilterRequest filter, CancellationToken cancellationToken = default);
    Task<UpdateLeadStatusResponse> MoveToStageAsync(int id, MoveLeadStageRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IDemoBookingService
{
    Task<DemoBookingResponse> CreateAsync(CreateDemoBookingRequest request, CancellationToken cancellationToken = default);
    Task<DemoSlotAvailabilityResponse> CheckAvailabilityAsync(string day, string slot, CancellationToken cancellationToken = default);
    Task<List<DemoBookingResponse>> GetByLeadIdAsync(int leadId, CancellationToken cancellationToken = default);
    Task<DemoBookingResponse> UpdateStatusAsync(int bookingId, UpdateDemoBookingStatusRequest request, CancellationToken cancellationToken = default);
}

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public interface IAnalyticsService
{
    Task<DashboardStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<LeadStatsResponse> GetLeadStatsAsync(CancellationToken cancellationToken = default);
    Task<List<LeadGrowthPoint>> GetLeadGrowthAsync(int days = 7, CancellationToken cancellationToken = default);
    Task<LeadAnalyticsResponse> GetLeadAnalyticsAsync(CancellationToken cancellationToken = default);
    void InvalidateDashboardCache();
}

public interface IUserService
{
    Task<List<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateRoleAsync(int userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateStatusAsync(int userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
}

public interface IEmailAutomationService
{
    Task TriggerWelcomeSequenceAsync(Lead lead, CancellationToken cancellationToken = default);
    Task TriggerDemoReminderAsync(Lead lead, CancellationToken cancellationToken = default);
    Task TriggerDemoConfirmationAsync(Lead lead, string day, string slot, CancellationToken cancellationToken = default);
    Task TriggerLeadFollowUpAsync(Lead lead, string followUpType, CancellationToken cancellationToken = default);
}

public interface IWhatsAppAutomationService
{
    Task SendLeadCapturedMessageAsync(Lead lead, CancellationToken cancellationToken = default);
    Task SendDemoReminderAsync(Lead lead, string day, string slot, CancellationToken cancellationToken = default);
    Task SendLeadFollowUpAsync(Lead lead, string followUpType, CancellationToken cancellationToken = default);
}

public interface IStaffNotificationService
{
    Task<int> NotifyLeadCreatedAsync(Lead lead, CancellationToken cancellationToken = default);
    Task NotifyAdminNewLeadAlertAsync(Lead lead, CancellationToken cancellationToken = default);
    Task NotifyStaffLeadAssignedAsync(Lead lead, AppUser staffUser, CancellationToken cancellationToken = default);
}

public interface IMarketingAutomationService
{
    Task TriggerLeadCreatedAsync(Lead lead, CancellationToken cancellationToken = default);
}

public interface IFollowUpScheduler
{
    Task ScheduleLeadFollowUpsAsync(Lead lead, CancellationToken cancellationToken = default);
}

public interface ILeadCreatedTrigger
{
    Task ExecuteAsync(Lead lead, CancellationToken cancellationToken = default);
}

public interface ITokenService
{
    string GenerateToken(AppUser user);
}

public interface IFunnelTrackingService
{
    Task TrackAsync(int leadId, string stage, CancellationToken cancellationToken = default);
    Task<FunnelStageCountResponse> GetCountsAsync(CancellationToken cancellationToken = default);
    Task<FunnelAnalyticsResponse> GetAnalyticsAsync(int days, CancellationToken cancellationToken = default);
}
