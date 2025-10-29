using Microsoft.EntityFrameworkCore;

namespace Mango.Services.RewardAPI.Data
{
    public class SqlServerAppDbContext : AppDbContext
    {
        public SqlServerAppDbContext(DbContextOptions<SqlServerAppDbContext> options) : base(options)
        {
        }
    }
}