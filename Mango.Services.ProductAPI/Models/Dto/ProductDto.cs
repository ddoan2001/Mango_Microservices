using Mango.Common.Extensions.Models;

namespace Mango.Services.ProductAPI.Models.Dto
{
    public class ProductDto : BaseEntityDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }
        public int QuantityInStock { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageLocalPath { get; set; }
        public IFormFile? File { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
    }
}
