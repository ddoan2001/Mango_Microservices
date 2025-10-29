using Mango.Common.Extensions.Models;

namespace Mango.Services.ProductAPI.Models.Dto.Filters
{
    public class CategoryDto : BaseEntityDto
    {
        public long CategoryId { get; set; }
        public required string Name { get; set; }
        public ICollection<ProductDto> Products { get; set; } = [];

    }
}
