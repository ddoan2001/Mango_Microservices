using Mango.Common.Extensions.Models;
using System.ComponentModel.DataAnnotations;

namespace Mango.Services.ProductAPI.Models
{
    public class Brand : BaseEntity
    {
        [Key]
        public int BrandId { get; set; }
        public required string Name { get; set; }
        public ICollection<Product> Products { get; set; } = [];
    }
}
