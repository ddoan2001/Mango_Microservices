using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ProductAPI.Data
{
    public class SqlServerAppDbContext : AppDbContext
    {
        public SqlServerAppDbContext(DbContextOptions<SqlServerAppDbContext> options) : base(options)
        {
        }
    }
}