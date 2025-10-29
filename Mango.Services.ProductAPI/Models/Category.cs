using Mango.Common.Extensions.Models;
using System.ComponentModel.DataAnnotations;

namespace Mango.Services.ProductAPI.Models
{
    public class Category : BaseEntity
    {
        [Key]
        public int CategoryId { get; set; }
        public required string Name { get; set; }
        public ICollection<Product> Products { get; set; } = [];
    }
}
