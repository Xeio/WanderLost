using Microsoft.AspNetCore.SignalR;
using WanderLost.Shared;

namespace WanderLost.Server.Controllers
{
    public class MerchantHub : Hub<IMerchantHubClient>, IMerchantHubClient
    {
        private readonly DataController _dataController;

        public MerchantHub(DataController dataController)
        {
            _dataController = dataController;
        }

        public async Task UpdateMerchant(string server, ActiveMerchant merchant)
        {
            throw new NotImplementedException(); //Validations and such

            await Clients.Group(server).UpdateMerchant(server, merchant);
        }

        public async Task SubscribeToServer(string server)

        {
            var regions = await _dataController.GetServerRegions();
            if (regions.SelectMany(r => r.Value.Servers).Any(s => server == s))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, server);
            }
        }        

        public async Task UnsubscribeFromServer(string server)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, server);
        }
    }
}
