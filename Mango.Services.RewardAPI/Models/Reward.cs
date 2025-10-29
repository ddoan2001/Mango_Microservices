using Mango.Common.Extensions.Models;

namespace Mango.Services.RewardAPI.Models
{
    public class Reward : BaseEntity
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public DateTime RewardsDate { get; set; }
        public int RewardsActivity { get; set; }
        public int OrderId { get; set; }
    }
}