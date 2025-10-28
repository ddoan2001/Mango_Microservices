using Mango.Common.Extensions.Interface;
using Mango.Services.AuthAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.AuthAPI.Data
{
    /// <summary>
    /// Custom database initializer for AuthAPI that implements the ICustomDatabaseInitializer interface
    /// This approach allows the shared extension to automatically detect and use this initializer
    /// </summary>
    public class AuthDatabaseInitializer : ICustomDatabaseInitializer
    {
        /// <summary>
        /// Initialize the Auth database with Identity-specific seeding
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="serviceProvider">The scoped service provider</param>
        /// <returns>Task</returns>
        public async Task InitializeAsync(DbContext context, IServiceProvider serviceProvider)
        {
            if (context is not AppDbContext authContext)
            {
                throw new ArgumentException($"Expected AppDbContext, but got {context.GetType().Name}");
            }

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await SeedRolesDataAsync(authContext);
            await SeedUsersDataAsync(authContext, userManager);
            await authContext.SaveChangesAsync();
        }

        private static async Task SeedRolesDataAsync(AppDbContext context)
        {
            if (!context.Roles.Any())
            {
                await context.Roles.AddRangeAsync(
                    new IdentityRole { Name = "CUSTOMER", NormalizedName = "CUSTOMER" },
                    new IdentityRole { Name = "ADMIN", NormalizedName = "ADMIN" }
                );
            }
        }

        private static async Task SeedUsersDataAsync(AppDbContext context, UserManager<ApplicationUser> userManager)
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