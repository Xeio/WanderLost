using Microsoft.AspNetCore.SignalR;
using WanderLost.Shared.Data;
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

            var serverMerchantGroup = activeMerchantGroups.FirstOrDefault(m => m.MerchantName == merchant.Name);

            if (serverMerchantGroup is null) return; //Failed to find matching merchant
            if (!serverMerchantGroup.IsActive) return; //Don't allow updating merchants that aren't active

            var clientIp = GetClientIp();
            //Only allow a user to upload one entry for a merchant. If they submit another, delete the original
            serverMerchantGroup.ActiveMerchants.RemoveAll(m => m.UploadedBy == clientIp); 

            merchant.UploadedBy = clientIp;
            serverMerchantGroup.UpdateOrAddMerchant(merchant);

            _logger.LogInformation("Updated server {server} merchant {Merchant}. Zone:{Zone}, Card:{Card}", server, merchant.Name, merchant.Zone, merchant.Card.Name);

            await Clients.Group(server).UpdateMerchantGroup(server, serverMerchantGroup);
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
            return activeMerchants.Where(m => m.ActiveMerchants.Any());
        }

        private string GetClientIp()
        {
            //Check for header added by Nginx proxy
            //Potential security concern if this is not hosted behind a proxy that sets X-Real-IP,
            //that a malicious user could inject this header to fake address. Maybe make this configurable?
            var headers = Context.GetHttpContext()?.Request.Headers;
            if(headers?["X-Real-IP"].ToString() is string realIp && !string.IsNullOrWhiteSpace(realIp))
            {
                return realIp;
            }

            //Fallback for dev environment
            var remoteAddr = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            return remoteAddr;
        }
    }
}
