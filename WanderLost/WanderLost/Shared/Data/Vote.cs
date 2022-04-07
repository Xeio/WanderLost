using System.ComponentModel.DataAnnotations;

namespace WanderLost.Shared.Data
{
    public class Vote
    {
        public Guid ActiveMerchantId { get; init; }
        public ActiveMerchant ActiveMerchant { get; init; } = new();

        [MaxLength(60)]
        public string ClientId { get; init; } = string.Empty;

        public VoteType VoteType { get; set; }
    }
}
