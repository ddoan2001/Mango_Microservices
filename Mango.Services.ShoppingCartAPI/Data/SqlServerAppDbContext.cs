using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ShoppingCartAPI.Data
{
    public class SqlServerAppDbContext : AppDbContext
    {
        public SqlServerAppDbContext(DbContextOptions<SqlServerAppDbContext> options) : base(options)
        {
        }
    }
}