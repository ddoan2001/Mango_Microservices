using Microsoft.AspNetCore.Identity;

namespace Mango.Common.Extensions.Models
{
    public abstract class BaseIdentityUser : IdentityUser
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}