using Mango.Common.Extensions.Models;
using System.ComponentModel.DataAnnotations;

namespace Mango.Services.ProductAPI.Models
{
    public class Product : BaseEntity
    {
        [Key]
        public int ProductId { get; set; }
        [Required]
        public required string Name { get; set; }
        [Range(1, 1000)]
        public double Price { get; set; }
        public string? Description { get; set; }
        public int QuantityInStock { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageLocalPath { get; set; }

        // foreign key
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }
    }
}
