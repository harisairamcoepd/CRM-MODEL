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
            db.AppUsers.AddRange(
                new AppUser { FullName = "COEPD Admin", Email = "admin@coepd.local", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", 12), Role = UserRole.Admin },
                new AppUser { FullName = "COEPD Staff", Email = "staff@coepd.local", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123", 12), Role = UserRole.Staff }
            );
        }

        if (!await db.Leads.AnyAsync())
        {
            db.Leads.AddRange(
                new Lead { Name = "Anusha Reddy", Phone = "9876543210", Email = "anusha@example.com", Location = "Hyderabad", Domain = "Business Analysis", Source = "Website", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new Lead { Name = "Rahul Sharma", Phone = "9988776655", Email = "rahul@example.com", Location = "Pune", Domain = "Data Analytics", Source = "Ads", CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new Lead { Name = "Sneha Patel", Phone = "9000011111", Email = "sneha@example.com", Location = "Bengaluru", Domain = "Product Management", Source = "Chatbot", CreatedAt = DateTime.UtcNow }
            );
        }

        await db.SaveChangesAsync();
    }
}
