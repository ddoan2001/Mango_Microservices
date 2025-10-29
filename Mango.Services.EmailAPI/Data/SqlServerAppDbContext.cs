using Microsoft.EntityFrameworkCore;

namespace Mango.Services.EmailAPI.Data
{
    public class SqlServerAppDbContext : AppDbContext
    {
        public SqlServerAppDbContext(DbContextOptions<SqlServerAppDbContext> options) : base(options)
        {
        }
    }
}