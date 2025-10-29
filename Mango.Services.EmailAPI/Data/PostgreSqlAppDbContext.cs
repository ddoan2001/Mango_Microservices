using Microsoft.EntityFrameworkCore;

namespace Mango.Services.EmailAPI.Data
{
    public class PostgreSqlAppDbContext : AppDbContext
    {
        public PostgreSqlAppDbContext(DbContextOptions<PostgreSqlAppDbContext> options) : base(options)
        {
        }
    }
}