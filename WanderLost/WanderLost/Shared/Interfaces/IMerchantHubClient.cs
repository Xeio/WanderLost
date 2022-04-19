using WanderLost.Shared.Data;

namespace WanderLost.Shared.Interfaces
{
    public interface IMerchantHubClient
    {
        Task UpdateMerchant(string server, ActiveMerchant merchant);
        Task UpdateMerchantGroup(string server, ActiveMerchantGroup merchantGroup);
        Task UpdateVoteTotal(Guid merchantId, int voteTotal);
        Task UpdateVoteSelf(Guid merchantId, VoteType voteType);
    }
}
