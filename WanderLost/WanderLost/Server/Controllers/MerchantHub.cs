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

            var activeMerchantGroups = await _dataController.GetActiveMerchantGroups(server);

            var serverMerchantGroup = activeMerchantGroups.FirstOrDefault(m => m.MostVotedMerchant?.Name == merchant.Name);

            if (serverMerchantGroup is null) return; //Failed to find matching merchant
            if (serverMerchantGroup.MostVotedMerchant?.NextAppearance > DateTimeOffset.UtcNow) return; //Don't allow updating merchants from the future

            serverMerchantGroup.ClearPlaceholderMerchant();
            serverMerchantGroup.UpdateOrAddMerchant(merchant);

            _logger.LogInformation("Updated server {server} merchant {Merchant}. Zone:{Zone}, Card:{Card}", server, merchant.Name, merchant.Zone, merchant.Card.Name);

            await UpdateMerchantGroup(server, serverMerchantGroup);
        }

        public async Task UpdateMerchantGroup(string server, ActiveMerchantGroup merchantGroup)
        {
            if (merchantGroup is null) return;
            if (!await IsValidServer(server)) return;

            await Clients.Group(server).UpdateMerchantGroup(server, merchantGroup);
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

        public async Task<IEnumerable<ActiveMerchantGroup>> GetKnownActiveMerchantGroups(string server)
        {
            var activeMerchants = await _dataController.GetActiveMerchantGroups(server);
            return activeMerchants.Where(m => m.Merchants.Any(x => !string.IsNullOrWhiteSpace(x.Zone)));
        }
    }
}
