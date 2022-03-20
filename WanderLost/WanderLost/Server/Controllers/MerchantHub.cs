using Microsoft.AspNetCore.SignalR;
using WanderLost.Shared;
using WanderLost.Shared.Interfaces;

namespace WanderLost.Server.Controllers
{
    public class MerchantHub : Hub<IMerchantHubClient>, IMerchantHubServer
    {
        public static string Path { get; } = "MerchantHub";

        private readonly DataController _dataController;
        private readonly ILogger _logger;

        public MerchantHub(DataController dataController, ILogger<MerchantHub> logger)
        {
            _dataController = dataController;
            _logger = logger;
        }

        public async Task UpdateMerchant(string server, ActiveMerchant merchant)
        {
            if (merchant is null) return;

            if (!await IsValidServer(server)) return;
            
            var allMerchantData = await _dataController.GetMerchantData();
            if (!merchant.IsValid(allMerchantData)) return;

            var activeMerchants = await _dataController.GetActiveMerchants(server);

            var serverMerchant = activeMerchants.FirstOrDefault(m => m.Name == merchant.Name);

            if (serverMerchant is null) return; //Failed to find matching merchant
            if (serverMerchant.NextAppearance > DateTimeOffset.UtcNow) return; //Don't allow updating merchants from the future

            serverMerchant.CopyInstance(merchant);

            _logger.LogInformation("Updated server {server} merchant {Merchant}. Zone:{Zone}, Card:{Card}", server, serverMerchant.Name, serverMerchant.Zone, serverMerchant.Card.Name);

            await Clients.Group(server).UpdateMerchant(server, serverMerchant);
        }

        public async Task SubscribeToServer(string server)
        {
            if (await IsValidServer(server))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, server);
            }
        }        

        public async Task UnsubscribeFromServer(string server)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, server);
        }

        private async Task<bool> IsValidServer(string server)
        {
            var regions = await _dataController.GetServerRegions();
            return regions.SelectMany(r => r.Value.Servers).Any(s => server == s);
        }

        public async Task<IEnumerable<ActiveMerchant>> GetKnownActiveMerchants(string server)
        {
            var activeMerchants = await _dataController.GetActiveMerchants(server);
            return activeMerchants.Where(m => !string.IsNullOrWhiteSpace(m.Zone));
        }
    }
}
