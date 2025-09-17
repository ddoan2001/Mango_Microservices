using Mango.Services.ProductAPI.RequestHelpers;

namespace Mango.Services.ProductAPI.Models.Dto
{
    /// <summary>
    /// Model to store cached product results with pagination metadata
    /// </summary>
    public class CachedProductResult
    {
        /// <summary>
        /// The product data
        /// </summary>
        public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();

        /// <summary>
        /// Pagination metadata
        /// </summary>
        public PaginationMetadata Metadata { get; set; } = new PaginationMetadata();
    }
}