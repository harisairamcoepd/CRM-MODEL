using COEPD.SalesFunnelSystem.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace COEPD.SalesFunnelSystem.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        var provider = Environment.GetEnvironmentVariable($"{DatabaseOptions.SectionName}__Provider");
        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var sqlitePath = Environment.GetEnvironmentVariable($"{DatabaseOptions.SectionName}__SqlitePath") ?? "App_Data/coepd-crm.db";
            optionsBuilder.UseSqlite($"Data Source={sqlitePath}");
            return new ApplicationDbContext(optionsBuilder.Options);
        }

        var sqlServerConnection =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            Environment.GetEnvironmentVariable("ConnectionStrings__SqlServerConnection") ??
            "Server=(localdb)\\mssqllocaldb;Database=COEPDSalesFunnelDb;Trusted_Connection=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(sqlServerConnection);
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
