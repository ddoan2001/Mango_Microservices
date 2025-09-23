using Mango.Services.AuthAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.AuthAPI.Data
{
    public class DbInitializer
    {
        public static async Task InitDb(WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>()
                ?? throw new InvalidOperationException("Failed to retrieve store context");

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
                ?? throw new InvalidOperationException("Failed to retrieve user manager");

            // migration for users table
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }
            await SeedRolesData(context);
            await SeedUsersData(context, userManager);
            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesData(AppDbContext context)
        {
            if (!context.Roles.Any())
            {
                await context.Roles.AddRangeAsync(
                    new IdentityRole { Name = "CUSTOMER", NormalizedName = "CUSTOMER" },
                    new IdentityRole { Name = "ADMIN", NormalizedName = "ADMIN" }
                );
            }
        }
        private static async Task SeedUsersData(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            if (!userManager.Users.Any())
            {
                var user = new ApplicationUser
                {
                    UserName = "danhdc2001@gmail.com",
                    Email = "danhdc2001@gmail.com",
                    Name = "danhdc2001",
                    PhoneNumber = "077123123"
                };

                await userManager.CreateAsync(user, "A@123456a");
                await userManager.AddToRoleAsync(user, "CUSTOMER");

                var admin = new ApplicationUser
                {
                    UserName = "admin@gmail.com",
                    Email = "admin@gmail.com",
                    Name = "admin",
                    PhoneNumber = "079456789"
                };

                await userManager.CreateAsync(admin, "A@123456a");
                await userManager.AddToRoleAsync(admin, "ADMIN");
            }

        }
    }
}
