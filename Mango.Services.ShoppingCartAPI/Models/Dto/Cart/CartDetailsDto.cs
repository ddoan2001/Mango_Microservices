using Mango.Common.Extensions.Models;

namespace Mango.Services.ShoppingCartAPI.Models.Dto.Cart
{
    public class CartDetailsDto : BaseEntityDto
    {
        public int CartDetailsId { get; set; }
        public int Quantity { get; set; }

        public int ProductId { get; set; }
        public ProductDto? Product { get; set; }

        public int CartHeaderId { get; set; }
    }
}
