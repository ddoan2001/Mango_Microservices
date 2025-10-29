using Microsoft.EntityFrameworkCore;

namespace Mango.Services.CouponAPI.Data
{
    public class PostgreSqlAppDbContext : AppDbContext
    {
        public PostgreSqlAppDbContext(DbContextOptions<PostgreSqlAppDbContext> options) : base(options)
        {
        }
    }
}
