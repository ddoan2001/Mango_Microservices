using System.Text.Json.Serialization;
using Mango.Common.Extensions.Models;

namespace Mango.Services.AuthAPI.Models
{
    public class Address : BaseEntity
    {
        [JsonIgnore]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Line1 { get; set; }
        public string? Line2 { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        [JsonPropertyName("postal_code")]
        public required string PostalCode { get; set; }
        public required string Country { get; set; }
    }
}
