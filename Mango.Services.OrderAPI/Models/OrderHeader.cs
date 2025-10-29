using Mango.Common.Extensions.Models;
using Mango.Services.OrderAPI.Utility;
using System.ComponentModel.DataAnnotations;

namespace Mango.Services.OrderAPI.Models
{
    public class OrderHeader : BaseEntity
    {
        [Key]
        public int OrderHeaderId { get; set; }
        public required string UserId { get; set; }
        public string? CouponCode { get; set; }
        public double Discount { get; set; }
        public double OrderTotal { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public double DeliveryFee { get; set; }
        public DateTime OrderTime { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string? PaymentIntentId { get; set; }
        public string? ClientSecret { get; set; }

        public PaymentSummary? PaymentSummary { get; set; }
        public ShippingAddress? ShippingAddress { get; set; }

        public List<OrderDetails> OrderDetails { get; set; } = [];
    }
}
