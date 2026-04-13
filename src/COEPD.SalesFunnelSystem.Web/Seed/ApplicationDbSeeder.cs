using COEPD.SalesFunnelSystem.Domain.Entities;
using COEPD.SalesFunnelSystem.Domain.Enums;
using COEPD.SalesFunnelSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace COEPD.SalesFunnelSystem.Web.Seed;

public static class ApplicationDbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (!await db.AppUsers.AnyAsync())
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isProduction = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);

            var adminEmail = Environment.GetEnvironmentVariable("COEPD_ADMIN_EMAIL");
            var adminPassword = Environment.GetEnvironmentVariable("COEPD_ADMIN_PASSWORD");
            var staffEmail = Environment.GetEnvironmentVariable("COEPD_STAFF_EMAIL");
            var staffPassword = Environment.GetEnvironmentVariable("COEPD_STAFF_PASSWORD");

            if (!isProduction)
            {
                adminEmail ??= "admin@coepd.local";
                adminPassword ??= "Admin@123";
                staffEmail ??= "staff@coepd.local";
                staffPassword ??= "Staff@123";
            }

            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                db.AppUsers.Add(new AppUser
                {
                    FullName = "COEPD Admin",
                    Email = adminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, 12),
                    Role = UserRole.Admin
                });
            }

            if (!string.IsNullOrWhiteSpace(staffEmail) && !string.IsNullOrWhiteSpace(staffPassword))
            {
                db.AppUsers.Add(new AppUser
                {
                    FullName = "COEPD Staff",
                    Email = staffEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(staffPassword, 12),
                    Role = UserRole.Staff
                });
            }
        }

        if (!await db.Leads.AnyAsync())
        {
            var defaultStaffId = await db.AppUsers
                .Where(x => x.Role == UserRole.Staff && x.IsActive)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            db.Leads.AddRange(
                new Lead { Name = "Anusha Reddy", Phone = "9876543210", Email = "anusha@example.com", Location = "Hyderabad", Domain = "Business Analysis", Source = "Website", AssignedStaffId = defaultStaffId, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new Lead { Name = "Rahul Sharma", Phone = "9988776655", Email = "rahul@example.com", Location = "Pune", Domain = "Data Analytics", Source = "Ads", AssignedStaffId = defaultStaffId, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new Lead { Name = "Sneha Patel", Phone = "9000011111", Email = "sneha@example.com", Location = "Bengaluru", Domain = "Product Management", Source = "Chatbot", AssignedStaffId = defaultStaffId, CreatedAt = DateTime.UtcNow }
            );
        }

        await db.SaveChangesAsync();
    }
}
