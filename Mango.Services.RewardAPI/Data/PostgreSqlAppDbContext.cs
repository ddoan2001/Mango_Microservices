using Microsoft.EntityFrameworkCore;

namespace Mango.Services.RewardAPI.Data
{
    public class PostgreSqlAppDbContext : AppDbContext
    {
        public PostgreSqlAppDbContext(DbContextOptions<PostgreSqlAppDbContext> options) : base(options)
        {
        }
    }
}