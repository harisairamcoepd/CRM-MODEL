namespace COEPD.SalesFunnelSystem.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryHours { get; set; } = 8;
}

public class EmailOptions
{
    public const string SectionName = "Email";
    public string Provider { get; set; } = "Smtp";
    public string FromName { get; set; } = "COEPD";
    public string FromEmail { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string SendGridApiKey { get; set; } = string.Empty;
}

public class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";
    public string ApiUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
}
