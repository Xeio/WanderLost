using Microsoft.AspNetCore.SignalR;
using WanderLost.Server.Data;
using WanderLost.Shared.Data;
using WanderLost.Shared.Interfaces;

namespace WanderLost.Server.Controllers
{
    public class MerchantHub : Hub<IMerchantHubClient>, IMerchantHubServer
    {
        public static string Path { get; } = "MerchantHub";

        private readonly DataController _dataController;

        public MerchantHub(DataController dataController)
        {
            _dataController = dataController;
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

            foreach(var existingMerchant in serverMerchantGroup.ActiveMerchants)
            {
                if (existingMerchant.IsEqualTo(merchant))
                {
                    //Found an existing matching merchant, so just upvote it instead
                    await Vote(server, existingMerchant.Id, VoteType.Upvote);
                    return;
                }
            }

            //Didn't find an existing entry, initialize the server entry and vote totals
            merchant.UploadedBy = clientIp;
            merchant.Id = Guid.NewGuid();
            merchant.Votes = 1;
            await _dataController.InitVoteGroup(server, merchant, new Data.Vote() { ClientId = clientIp, VoteType = VoteType.Upvote });
            serverMerchantGroup.ActiveMerchants.Add(merchant);

            await Clients.Group(server).UpdateMerchantGroup(server, serverMerchantGroup);
        }

        public async Task Vote(string server, Guid merchantId, VoteType voteType)
        {
            if(_dataController.TryGetVoteGroupByMerchantId(merchantId, out var voteGroup))
            {
                var clientId = GetClientIp();
                var existingVote = voteGroup.Votes.FirstOrDefault(v => v.ClientId == clientId);
                if (existingVote is not null && existingVote.VoteType != voteType)
                {
                    existingVote.VoteType = voteType;
                    voteGroup.Merchant.Votes = voteGroup.Votes.Sum(v => (int)v.VoteType);
                    await Clients.Group(server).UpdateVoteTotal(merchantId, voteGroup.Merchant.Votes);
                }
                else if (existingVote is null)
                {
                    var newVote = new Data.Vote()
                    {
                        ClientId = clientId,
                        VoteType = voteType
                    };
                    voteGroup.Votes.Add(newVote);
                    voteGroup.Merchant.Votes = voteGroup.Votes.Sum(v => (int)v.VoteType);
                    await Clients.Group(server).UpdateVoteTotal(merchantId, voteGroup.Merchant.Votes);
                }
            }
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
#if DEBUG
            //In debug mode, allow using the connection ID to simulate multiple clients
            return Context.ConnectionId;
#endif

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
