namespace COEPD.SalesFunnelSystem.Web.ViewModels;

public class LandingPageViewModel
{
    public List<LandingDomainOption> FeaturedDomains { get; set; } = [];
    public List<LandingDomainOption> Domains { get; set; } = [];
}

public class LandingDomainOption
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
