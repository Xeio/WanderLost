using WanderLost.Shared.Data;

namespace WanderLost.Server.Data
{
    public class Vote
    {
        public string ClientId { get; init; } = string.Empty;
        public VoteType VoteType { get; set; }
    }
}
