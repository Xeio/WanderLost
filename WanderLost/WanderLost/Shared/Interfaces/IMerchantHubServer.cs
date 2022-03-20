﻿namespace WanderLost.Shared.Interfaces
{
    public interface IMerchantHubServer : IMerchantHubShared
    {
        Task SubscribeToServer(string server);
        Task UnsubscribeFromServer(string server);
        Task<IEnumerable<ActiveMerchantGroup>> GetKnownActiveMerchantGroups(string server);
    }
}
