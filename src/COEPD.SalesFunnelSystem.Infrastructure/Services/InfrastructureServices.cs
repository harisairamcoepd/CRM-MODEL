using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using COEPD.SalesFunnelSystem.Application.Services;
using COEPD.SalesFunnelSystem.Domain.Entities;
using COEPD.SalesFunnelSystem.Domain.Enums;
using COEPD.SalesFunnelSystem.Infrastructure.Data;
using COEPD.SalesFunnelSystem.Infrastructure.Options;
using COEPD.SalesFunnelSystem.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace COEPD.SalesFunnelSystem.Infrastructure.Services;

public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public string GenerateToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: DateTime.UtcNow.AddHours(_options.ExpiryHours), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class EmailAutomationService : IEmailAutomationService
{
    private readonly EmailOptions _options;
    private readonly ApplicationDbContext _db;
    public EmailAutomationService(IOptions<EmailOptions> options, ApplicationDbContext db) { _options = options.Value; _db = db; }

    public async Task TriggerWelcomeSequenceAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        const string subject = "COEPD Admissions Update";
        var body = $"Hi {lead.Name}, thanks for contacting COEPD. Our advisor will reach you.";
        var deliveryStatus = await SendEmailAsync(lead.Email, subject, body, cancellationToken);
        _db.EmailAutomationLogs.Add(new EmailAutomationLog { LeadId = lead.Id, TemplateKey = "lead-created", Subject = subject, Status = deliveryStatus });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task TriggerDemoReminderAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        const string subject = "Your COEPD demo is confirmed";
        const string body = "Your demo booking is confirmed. We will share the next steps shortly.";
        var deliveryStatus = await SendEmailAsync(lead.Email, subject, body, cancellationToken);
        _db.EmailAutomationLogs.Add(new EmailAutomationLog { LeadId = lead.Id, TemplateKey = "demo-reminder", Subject = subject, Status = deliveryStatus });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task TriggerLeadFollowUpAsync(Lead lead, string followUpType, CancellationToken cancellationToken = default)
    {
        var subject = followUpType == FollowUpJobTypes.OneHour
            ? "Need help choosing your COEPD track?"
            : "Your COEPD learning roadmap is ready";

        var body = followUpType == FollowUpJobTypes.OneHour
            ? $"Hi {lead.Name}, sharing quick guidance to help you decide your best-fit domain."
            : $"Hi {lead.Name}, this is a one-day follow-up to help you plan your next step with COEPD.";

        var deliveryStatus = await SendEmailAsync(lead.Email, subject, body, cancellationToken);
        _db.EmailAutomationLogs.Add(new EmailAutomationLog
        {
            LeadId = lead.Id,
            TemplateKey = $"followup-{followUpType.ToLowerInvariant()}",
            Subject = subject,
            Status = deliveryStatus
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        if (_options.Provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(_options.SendGridApiKey))
        {
            var client = new SendGridClient(_options.SendGridApiKey);
            var message = MailHelper.CreateSingleEmail(new EmailAddress(_options.FromEmail, _options.FromName), new EmailAddress(to), subject, body, $"<p>{body}</p>");
            await client.SendEmailAsync(message, cancellationToken);
            return "Sent";
        }

        if (string.IsNullOrWhiteSpace(_options.SmtpHost) || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            // Explicit mock-mode email delivery for local and demo environments.
            await Task.Delay(30, cancellationToken);
            return "Simulated-Sent";
        }

        using var clientSmtp = new SmtpClient(_options.SmtpHost, _options.SmtpPort) { EnableSsl = _options.EnableSsl, Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword) };
        using var mail = new MailMessage(_options.FromEmail, to, subject, body);
        await clientSmtp.SendMailAsync(mail, cancellationToken);
        return "Sent";
    }
}

public class WhatsAppAutomationService : IWhatsAppAutomationService
{
    private readonly ApplicationDbContext _db;
    public WhatsAppAutomationService(ApplicationDbContext db) => _db = db;

    public Task SendLeadCapturedMessageAsync(Lead lead, CancellationToken cancellationToken = default) =>
        SendAsync(lead, "lead-captured", $"Hi {lead.Name}, thank you for contacting COEPD about {lead.Domain}.", cancellationToken);

    public Task SendDemoReminderAsync(Lead lead, string day, string slot, CancellationToken cancellationToken = default) =>
        SendAsync(lead, "demo-reminder", $"Your COEPD demo is booked for {day} - {slot}.", cancellationToken);

    public Task SendLeadFollowUpAsync(Lead lead, string followUpType, CancellationToken cancellationToken = default)
    {
        var message = followUpType == FollowUpJobTypes.OneHour
            ? $"Hi {lead.Name}, quick follow-up from COEPD. Need help finalizing your domain?"
            : $"Hi {lead.Name}, one-day follow-up from COEPD. Shall we schedule your counseling call?";

        return SendAsync(lead, $"followup-{followUpType.ToLowerInvariant()}", message, cancellationToken);
    }

    private async Task SendAsync(Lead lead, string type, string message, CancellationToken cancellationToken)
    {
        // Simulate WhatsApp API call for reliable local development and deterministic behavior.
        await Task.Delay(80, cancellationToken);
        _db.WhatsAppMessageLogs.Add(new WhatsAppMessageLog { LeadId = lead.Id, MessageType = type, Phone = lead.Phone, Status = "Simulated-Sent" });
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class StaffNotificationService : IStaffNotificationService
{
    private readonly IUserRepository _userRepository;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<StaffNotificationService> _logger;

    public StaffNotificationService(
        IUserRepository userRepository,
        IOptions<EmailOptions> emailOptions,
        ILogger<StaffNotificationService> logger)
    {
        _userRepository = userRepository;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task<int> NotifyLeadCreatedAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var recipients = users
            .Where(x => x.IsActive &&
                        (x.Role == UserRole.Staff || x.Role == UserRole.Admin) &&
                        !string.IsNullOrWhiteSpace(x.Email))
            .Select(x => x.Email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var recipient in recipients)
        {
            try
            {
                await SendInternalNotificationAsync(recipient, lead, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify staff user {Email} for lead {LeadId}.", recipient, lead.Id);
            }
        }

        _logger.LogInformation("Staff notification completed for lead {LeadId}. Recipients={Count}.", lead.Id, recipients.Count);
        return recipients.Count;
    }

    private async Task SendInternalNotificationAsync(string recipientEmail, Lead lead, CancellationToken cancellationToken)
    {
        var subject = $"New Lead Alert: {lead.Name} ({lead.Domain})";
        var body = $"New lead captured. Name: {lead.Name}, Phone: {lead.Phone}, Domain: {lead.Domain}, Source: {lead.Source}.";

        if (_emailOptions.Provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(_emailOptions.SendGridApiKey) &&
            !string.IsNullOrWhiteSpace(_emailOptions.FromEmail))
        {
            var client = new SendGridClient(_emailOptions.SendGridApiKey);
            var message = MailHelper.CreateSingleEmail(
                new EmailAddress(_emailOptions.FromEmail, _emailOptions.FromName),
                new EmailAddress(recipientEmail),
                subject,
                body,
                $"<p>{body}</p>");
            await client.SendEmailAsync(message, cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(_emailOptions.SmtpHost) || string.IsNullOrWhiteSpace(_emailOptions.FromEmail))
        {
            // Fall back to simulated notification in local/dev environments without mail transport.
            await Task.Delay(20, cancellationToken);
            return;
        }

        using var clientSmtp = new SmtpClient(_emailOptions.SmtpHost, _emailOptions.SmtpPort)
        {
            EnableSsl = _emailOptions.EnableSsl,
            Credentials = new NetworkCredential(_emailOptions.SmtpUsername, _emailOptions.SmtpPassword)
        };
        using var mail = new MailMessage(_emailOptions.FromEmail, recipientEmail, subject, body);
        await clientSmtp.SendMailAsync(mail, cancellationToken);
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));
        var connectionString = NormalizeConnectionString(configuration.GetConnectionString("DefaultConnection"));
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IDemoBookingRepository, DemoBookingRepository>();
        services.AddScoped<IFunnelEventRepository, FunnelEventRepository>();
        services.AddScoped<ILeadActivityRepository, LeadActivityRepository>();
        services.AddScoped<ILeadFollowUpJobRepository, LeadFollowUpJobRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IDemoBookingService, DemoBookingService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IFunnelTrackingService, FunnelTrackingService>();
        services.AddScoped<IMarketingAutomationService, MarketingAutomationService>();
        services.AddScoped<IFollowUpScheduler, FollowUpScheduler>();
        services.AddScoped<ILeadCreatedTrigger, LeadCreatedActivityTrigger>();
        services.AddScoped<ILeadCreatedTrigger, LeadCreatedEmailTrigger>();
        services.AddScoped<ILeadCreatedTrigger, LeadCreatedWhatsAppTrigger>();
        services.AddScoped<ILeadCreatedTrigger, LeadCreatedFollowUpScheduleTrigger>();
        services.AddScoped<ILeadCreatedTrigger, LeadCreatedStaffNotificationTrigger>();
        services.AddScoped<IEmailAutomationService, EmailAutomationService>();
        services.AddScoped<IWhatsAppAutomationService, WhatsAppAutomationService>();
        services.AddScoped<IStaffNotificationService, StaffNotificationService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddHostedService<LeadFollowUpProcessor>();
        return services;
    }

    private static string NormalizeConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        }

        var normalized = connectionString;
        var hasSqlexpress = LocalSqlInstanceExists("SQLEXPRESS");
        var hasSqlexpess = LocalSqlInstanceExists("SQLEXPESS");

        if (normalized.Contains(@".\SQLEXPRESS", StringComparison.OrdinalIgnoreCase) && !hasSqlexpress && hasSqlexpess)
        {
            normalized = normalized.Replace(@".\SQLEXPRESS", @".\SQLEXPESS", StringComparison.OrdinalIgnoreCase);
        }
        else if (normalized.Contains(@".\SQLEXPESS", StringComparison.OrdinalIgnoreCase) && !hasSqlexpess && hasSqlexpress)
        {
            normalized = normalized.Replace(@".\SQLEXPESS", @".\SQLEXPRESS", StringComparison.OrdinalIgnoreCase);
        }

        return normalized;
    }

    private static bool LocalSqlInstanceExists(string instanceName)
    {
        try
        {
            var serviceName = $"MSSQL${instanceName}";
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"query \"{serviceName}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);
            return process.ExitCode == 0 && output.Contains(serviceName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
