using Mango.Common.Extensions.Models;

namespace Mango.Services.AuthAPI.Models
{
    public class ApplicationUser : BaseIdentityUser
    {
        public string Name { get; set; }
        public int? AddressId { get; set; }
        public Address? Address { get; set; }
    }
}
