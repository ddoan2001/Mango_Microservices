using Microsoft.EntityFrameworkCore;
using Mango.Common.Extensions.Data;

namespace Mango.Services.OrderAPI.Data
{
    public class PostgreSqlAppDbContext : AppDbContext
    {
        public PostgreSqlAppDbContext(DbContextOptions<PostgreSqlAppDbContext> options) : base(options)
        {
        }
    }
}