using Mango.Common.Extensions.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mango.Services.ShoppingCartAPI.Models
{
    public class CartHeader : BaseEntity
    {
        [Key]
        public int CartHeaderId { get; set; }

        public string? UserId { get; set; }
        public string? CouponCode { get; set; }
        public List<CartDetails> CartDetails { get; set; } = [];

        [NotMapped]
        public double Discount { get; set; }
        [NotMapped]
        public double CartTotal { get; set; }

        public void AddItem(ProductDto product, int quantity)
        {
            if (product == null) ArgumentNullException.ThrowIfNull(product);

            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

            var existingItem = FindCartDetails(product.ProductId);

            if (existingItem == null)
            {
                CartDetails.Add(new CartDetails
                {
                    ProductId = product.ProductId,
                    Quantity = quantity
                });
            }
            else
            {
                existingItem.Quantity += quantity;
            }
        }

        public void RemoveItem(int productId, int quantity)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity should be greater than zero", nameof(quantity));

            var item = FindCartDetails(productId);
            if (item == null) return;

            item.Quantity -= quantity;
            if (item.Quantity <= 0) CartDetails.Remove(item);
        }

        public void RemoveItems(int[] productIds)
        {
            foreach (var productId in productIds)
            {
                var item = FindCartDetails(productId);
                if (item == null) return;
                CartDetails.Remove(item);
            }
        }

        private CartDetails? FindCartDetails(int productId)
        {
            return CartDetails.FirstOrDefault(item => item.ProductId == productId);
        }
    }
}
