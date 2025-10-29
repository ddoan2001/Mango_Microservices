using Mango.Services.OrderAPI.Models;
using Microsoft.EntityFrameworkCore;
using Mango.Common.Extensions.Data;

namespace Mango.Services.OrderAPI.Data
{
    public class AppDbContext : BaseAppDbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetails> OrderDetails { get; set; }
    }
}
