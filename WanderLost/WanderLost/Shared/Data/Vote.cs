using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WanderLost.Shared.Data
{
    public class Vote
    {
        public Guid ActiveMerchantId { get; init; }

        [JsonIgnore]
        public ActiveMerchant ActiveMerchant { get; init; } = new();

        [JsonIgnore]
        [MaxLength(60)]
        public string ClientId { get; init; } = string.Empty;

        public VoteType VoteType { get; set; }
    }
}
