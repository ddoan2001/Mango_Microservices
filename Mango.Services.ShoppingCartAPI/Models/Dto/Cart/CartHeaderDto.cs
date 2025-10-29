using Mango.Common.Extensions.Models;

namespace Mango.Services.ShoppingCartAPI.Models.Dto.Cart
{
    public class CartHeaderDto : BaseEntityDto
    {
        public int CartHeaderId { get; set; }
        public string? UserId { get; set; }
        public List<CartDetailsDto> CartDetails { get; set; } = [];
        public string? CouponCode { get; set; }
        public double Discount { get; set; }
        public double CartTotal { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
