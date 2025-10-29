using Microsoft.EntityFrameworkCore;
using Mango.Common.Extensions.Data;

namespace Mango.Services.OrderAPI.Data
{
    public class SqlServerAppDbContext : AppDbContext
    {
        public SqlServerAppDbContext(DbContextOptions<SqlServerAppDbContext> options) : base(options)
        {
        }
    }
}