using Mango.Services.CouponAPI.Models;
using Microsoft.EntityFrameworkCore;
using Mango.Common.Extensions.Data;

namespace Mango.Services.CouponAPI.Data
{
    public class AppDbContext : BaseAppDbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Coupon> Coupons { get; set; }
    }
}
