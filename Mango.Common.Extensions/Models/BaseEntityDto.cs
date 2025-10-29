namespace Mango.Common.Extensions.Models
{
    public abstract class BaseEntityDto
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}