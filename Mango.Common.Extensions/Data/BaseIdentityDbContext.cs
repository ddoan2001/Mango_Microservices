using Mango.Common.Extensions.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Mango.Common.Extensions.Data
{
    public abstract class BaseIdentityDbContext<TUser> : IdentityDbContext<TUser>
        where TUser : IdentityUser
    {
        protected BaseIdentityDbContext(DbContextOptions options) : base(options)
        {
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            // Handle BaseEntity timestamp tracking
            var baseEntityEntries = ChangeTracker.Entries<BaseEntity>();
            foreach (var entry in baseEntityEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.Now;
                    entry.Entity.UpdatedAt = DateTime.Now;
                }
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.Now;
                }
            }

            // Handle BaseIdentityUser timestamp tracking
            var identityUserEntries = ChangeTracker.Entries<BaseIdentityUser>();
            foreach (var entry in identityUserEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.Now;
                    entry.Entity.UpdatedAt = DateTime.Now;
                }
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.Now;
                }
            }
        }
    }
}