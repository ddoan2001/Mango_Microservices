using Mango.Common.Extensions.Models;
using Mango.Services.OrderAPI.Models.Dto.Product;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mango.Services.OrderAPI.Models
{
    public class OrderDetails : BaseEntity
    {
        [Key]
        public int OrderDetailsId { get; set; }

        public int ProductId { get; set; }
        [NotMapped]
        public ProductDto? Product { get; set; }

        public int Quantity { get; set; }
        public string ProductName { get; set; }
        public double Price { get; set; }

        public int OrderHeaderId { get; set; }
        [ForeignKey("OrderHeaderId")]
        public OrderHeader? OrderHeader { get; set; }
    }
}
