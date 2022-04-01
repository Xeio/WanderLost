using WanderLost.Shared.Data;

namespace WanderLost.Server.Data
{
    public class VoteGroup
    {
        public ActiveMerchant Merchant { get; init; } = new();
        public List<Vote> Votes { get; init; } = new();
    }
}
