using WanderLost.Shared.Data;

namespace WanderLost.Shared.Interfaces;

public interface IMerchantHubClient
{
    Task UpdateMerchantGroup(string server, ActiveMerchantGroup merchantGroup);
    Task UpdateVoteSelf(Guid merchantId, VoteType voteType);
    Task UpdateVotes(List<MerchantVoteUpdate> merchantVoteUpdates);
}
