using System.ComponentModel.DataAnnotations;
using Mango.Common.Extensions.Models;

namespace Mango.Services.CouponAPI.Models
{
    public class Coupon : BaseEntity
    {
        [Key]
        public int CouponId { get; set; }
        [Required]
        public string CouponCode { get; set; }
        [Required]
        public double DiscountAmount { get; set; }
        public int MinAmount { get; set; }
    }
}
