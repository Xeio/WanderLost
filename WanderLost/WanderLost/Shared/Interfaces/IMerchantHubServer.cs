﻿using WanderLost.Shared.Data;

namespace WanderLost.Shared.Interfaces
{
    public interface IMerchantHubServer : IMerchantHubShared
    {
        Task SubscribeToServer(string server);
        Task UnsubscribeFromServer(string server);
        Task<IEnumerable<ActiveMerchantGroup>> GetKnownActiveMerchantGroups(string server);
        Task RequestClientVotes(string server);
        Task Vote(string server, Guid merchantId, VoteType voteType);
        Task<bool> HasNewerClient(int version);
    }
}
