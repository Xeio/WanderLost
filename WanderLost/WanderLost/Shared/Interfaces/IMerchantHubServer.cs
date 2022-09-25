using WanderLost.Shared.Data;

namespace WanderLost.Shared.Interfaces;

public interface IMerchantHubServer
{
    Task UpdateMerchant(string server, ActiveMerchant merchant);
    Task SubscribeToServer(string server);
    Task UnsubscribeFromServer(string server);
    Task<IEnumerable<ActiveMerchantGroup>> GetKnownActiveMerchantGroups(string server);
    Task<IEnumerable<Vote>> RequestClientVotes(string server);
    Task Vote(string server, Guid merchantId, VoteType voteType);
    Task<bool> HasNewerClient(int version);
    Task<PushSubscription?> GetPushSubscription(string clientToken);
    Task UpdatePushSubscription(PushSubscription subscription);
    Task RemovePushSubscription(string clientToken);
    Task<ProfileStats> GetProfileStats();
    Task<WeiStats> GetWeiStats();
    Task<List<LeaderboardEntry>> GetLeaderboard(string? server);
    Task UpdateDisplayName(string? displayName);
}
