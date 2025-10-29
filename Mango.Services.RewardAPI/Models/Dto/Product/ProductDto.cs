using Mango.Common.Extensions.Models;

namespace Mango.Services.RewardAPI.Models.Dto.Product
{
    public class ProductDto : BaseEntityDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public long Price { get; set; }
        public string Description { get; set; }
        public int QuantityInStock { get; set; }
        public string? ImageUrl { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
    }
}
