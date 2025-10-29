using Mango.Common.Extensions.Interface;
using Mango.Services.ProductAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ProductAPI.Data
{
    public class ProductDatabaseInitializer : ICustomDatabaseInitializer
    {
        public async Task InitializeAsync(DbContext context, IServiceProvider serviceProvider)
        {
            if (context is not AppDbContext appContext)
                return;

            await SeedCatesData(appContext);
            await SeedBrandsData(appContext);
            await SeedProductsData(appContext);
            await appContext.SaveChangesAsync();
        }

        private static async Task SeedCatesData(AppDbContext context)
        {
            if (context.Categories.Any()) return;
            var categories = new List<Category>
            {
                new() { Name = "Food" },
                new() { Name = "Water" }
            };
            await context.Categories.AddRangeAsync(categories);
        }

        private static async Task SeedBrandsData(AppDbContext context)
        {
            if (context.Brands.Any()) return;
            var brands = new List<Brand>
            {
                new() { Name = "KFC" },
                new() { Name = "Jollibee" }
            };
            await context.Brands.AddRangeAsync(brands);
        }

        private static async Task SeedProductsData(AppDbContext context)
        {
            if (context.Products.Any()) return;
            var products = new List<Product>
            {
                new()
                {
                    Name = "Samosa",
                    Price = 15,
                    Description = " Quisque vel lacus ac magna, vehicula sagittis ut non lacus.<br/> Vestibulum arcu turpis, maximus malesuada neque. Phasellus commodo cursus pretium.",
                    ImageUrl = "https://placehold.co/603x403",
                    QuantityInStock = 100
                },
                new()
                {
                    Name = "Paneer Tikka",
                    Price = 13.99,
                    Description = " Quisque vel lacus ac magna, vehicula sagittis ut non lacus.<br/> Vestibulum arcu turpis, maximus malesuada neque. Phasellus commodo cursus pretium.",
                    ImageUrl = "https://placehold.co/602x402",
                    QuantityInStock = 100
                },
                new()
                {
                    Name = "Sweet Pie",
                    Price = 10.99,
                    Description = " Quisque vel lacus ac magna, vehicula sagittis ut non lacus.<br/> Vestibulum arcu turpis, maximus malesuada neque. Phasellus commodo cursus pretium.",
                    ImageUrl = "https://placehold.co/601x401",
                    QuantityInStock = 100
                },
                new()
                {
                    Name = "Pav Bhaji",
                    Price = 15,
                    Description = " Quisque vel lacus ac magna, vehicula sagittis ut non lacus.<br/> Vestibulum arcu turpis, maximus malesuada neque. Phasellus commodo cursus pretium.",
                    ImageUrl = "https://placehold.co/600x400",
                    QuantityInStock = 100
                }
            };

            await context.Products.AddRangeAsync(products);
        }
    }
}