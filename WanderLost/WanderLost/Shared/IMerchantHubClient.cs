using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WanderLost.Shared
{
    public interface IMerchantHubClient
    {
        Task UpdateMerchant(string server, ActiveMerchant merchant);
        Task SubscribeToServer(string server);
        Task UnsubscribeFromServer(string server);
    }
}
