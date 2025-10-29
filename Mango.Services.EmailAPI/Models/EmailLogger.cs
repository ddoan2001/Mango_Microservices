using Mango.Common.Extensions.Models;

namespace Mango.Services.EmailAPI.Models
{
    public class EmailLogger : BaseEntity
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
    }
}
