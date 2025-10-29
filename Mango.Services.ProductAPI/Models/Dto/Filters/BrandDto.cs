using Mango.Common.Extensions.Models;

namespace Mango.Services.ProductAPI.Models.Dto.Filters
{
    public class BrandDto : BaseEntityDto
    {
        public long BrandId { get; set; }
        public required string Name { get; set; }
        public ICollection<ProductDto> Products { get; set; } = [];
    }
}
