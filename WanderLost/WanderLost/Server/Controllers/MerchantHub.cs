using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WanderLost.Server.Data;
using WanderLost.Shared;
using WanderLost.Shared.Data;
using WanderLost.Shared.Interfaces;

namespace WanderLost.Server.Controllers
{
    public class MerchantHub : Hub<IMerchantHubClient>, IMerchantHubServer
    {
        public static string Path { get; } = "MerchantHub";

        private readonly DataController _dataController;
        private readonly MerchantsDbContext _merchantsDbContext;

        public MerchantHub(DataController dataController, MerchantsDbContext merchantsDbContext)
        {
            _dataController = dataController;
            _merchantsDbContext = merchantsDbContext;
        }

        public async Task UpdateMerchant(string server, ActiveMerchant merchant)
        {
            if (merchant is null) return;

            if (!await IsValidServer(server)) return;

            var allMerchantData = await _dataController.GetMerchantData();
            if (!merchant.IsValid(allMerchantData)) return;

            var merchantGroup = await _merchantsDbContext.MerchantGroups
                .Where(g => g.Server == server && g.MerchantName == merchant.Name && g.AppearanceExpires > DateTimeOffset.Now)
                .Select(g => new ActiveMerchantGroup()
                {
                    Id = g.Id,
                    MerchantName = g.MerchantName,
                    AppearanceExpires = g.AppearanceExpires,
                    NextAppearance = g.NextAppearance,
                    Server = g.Server,
                    ActiveMerchants = g.ActiveMerchants.Where(m => !m.Hidden).ToList(),
                })
                .FirstOrDefaultAsync();

            if(merchantGroup == null)
            {
                //Merchant hasn't been saved to DB yet, get the in-memory one with expiration data calculated
                var activeMerchantGroups = await _dataController.GetActiveMerchantGroups(server);
                merchantGroup = activeMerchantGroups.FirstOrDefault(m => m.MerchantName == merchant.Name);

                //Don't allow modifying inactive merchants
                if (merchantGroup is null || !merchantGroup.IsActive) return;

                //Add it to the DB context to save later
                merchantGroup.Server = server;
                merchantGroup.MerchantName = merchant.Name;
            }

            var clientIp = GetClientIp();

            //If a client already uploaded a merchant, ignore further uploads and just skip out
            if (merchantGroup.ActiveMerchants.Any(m => m.UploadedBy == clientIp)) return;

            foreach (var existingMerchant in merchantGroup.ActiveMerchants)
            {
                if (existingMerchant.IsEqualTo(merchant))
                {
                    //Found an existing matching merchant, so just upvote it instead
                    await Vote(server, existingMerchant.Id, VoteType.Upvote);
                    return;
                }
            }

            //Because we did a custom select, the entity won't be attached by default, attach before modifying
            _merchantsDbContext.Attach(merchantGroup);

            //Special handling case for banned users
            if (await HandleBans(clientIp, server, merchantGroup, merchant)) return;

            //If this client is uploading to multiple servers, ignore them
            if (await ClientHasOtherServerUploads(server, clientIp)) return;

            merchant.UploadedBy = clientIp;
            merchantGroup.ActiveMerchants.Add(merchant);

            await _merchantsDbContext.SaveChangesAsync();

            await Clients.Group(server).UpdateMerchantGroup(server, merchantGroup);
        }

        private async Task<bool> HandleBans(string clientId, string server, ActiveMerchantGroup group, ActiveMerchant merchant)
        {            
            //Skip out if no bans
            if (!_merchantsDbContext.Bans.Any(b => b.ClientId == clientId && b.ExpiresAt > DateTimeOffset.Now)) return false;

            //Create a hidden merchant only visible to this client
            var hiddenMerchant = new ActiveMerchant()
            {
                Card = merchant.Card,
                Name = merchant.Name,
                RapportRarity = merchant.RapportRarity,
                UploadedBy = clientId,
                Zone = merchant.Zone,
                Hidden = true,
            };

            group.ActiveMerchants.Add(hiddenMerchant);

            await _merchantsDbContext.SaveChangesAsync();

            await Clients.Caller.UpdateMerchantGroup(server, group);

            //Fake some votes over the next couple minutes, probably overkill
            await Task.Delay(30 + Random.Shared.Next(1, 15) * 1000);

            int maxVotes = Random.Shared.Next(4, 13);
            for (int i = 0; i < maxVotes; i++)
            {
                hiddenMerchant.Votes--;

                await _merchantsDbContext.SaveChangesAsync();

                await Clients.Caller.UpdateVoteTotal(hiddenMerchant.Id, hiddenMerchant.Votes);

                await Task.Delay(Random.Shared.Next(5, 30) * 1000);
            }

            return true;
        }

        private async Task<bool> ClientHasOtherServerUploads(string originalServer, string clientId)
        {
            return await _merchantsDbContext.MerchantGroups
                .Where(g => g.Server != originalServer && g.AppearanceExpires > DateTimeOffset.Now)
                .SelectMany(g => g.ActiveMerchants.Where(m => m.UploadedBy == clientId))
                .AnyAsync();
        }

        public async Task Vote(string server, Guid merchantId, VoteType voteType)
        {
            var activeMerchant = await _merchantsDbContext.ActiveMerchants
                .Include(m => m.ClientVotes)
                .SingleOrDefaultAsync(m => m.Id == merchantId);
            if (activeMerchant == null) return;

            var clientId = GetClientIp();

            //Don't let a user vote on their own submission to make some aggregation stuff easier later
            if (activeMerchant.UploadedBy == clientId) return;

            var existingVote = activeMerchant.ClientVotes.FirstOrDefault(v => v.ClientId == clientId);
            if(existingVote == null)
            {
                activeMerchant.ClientVotes.Add(new Vote()
                {
                    ActiveMerchant = activeMerchant,
                    ClientId = clientId,
                    VoteType = voteType,
                });
                activeMerchant.Votes = activeMerchant.ClientVotes.Sum(v => (int)v.VoteType);

                await _merchantsDbContext.SaveChangesAsync();

                await Clients.Group(server).UpdateVoteTotal(merchantId, activeMerchant.Votes);
                await Clients.Caller.UpdateVoteSelf(merchantId, voteType);
            }
            else if(existingVote.VoteType != voteType)
            {
                existingVote.VoteType = voteType;
                activeMerchant.Votes = activeMerchant.ClientVotes.Sum(v => (int)v.VoteType);

                await _merchantsDbContext.SaveChangesAsync();

                await Clients.Group(server).UpdateVoteTotal(merchantId, activeMerchant.Votes);
                await Clients.Caller.UpdateVoteSelf(merchantId, voteType);
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
            var clientIp = GetClientIp();
            return await _merchantsDbContext.MerchantGroups
                .Where(g => g.Server == server && g.AppearanceExpires > DateTimeOffset.Now)
                .Select(mg => new ActiveMerchantGroup
                {
                     Server = mg.Server,
                     MerchantName = mg.MerchantName,
                     ActiveMerchants = mg.ActiveMerchants
                                            .Where(m => !m.Hidden || m.UploadedBy == clientIp)
                                            .ToList(),
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Vote>> RequestClientVotes(string server)
        {
            var clientIp = GetClientIp();
            return await _merchantsDbContext.MerchantGroups
                .Where(g => g.Server == server && g.AppearanceExpires > DateTimeOffset.Now)
                .SelectMany(mg => mg.ActiveMerchants.SelectMany(m => m.ClientVotes.Where(vote => vote.ClientId == clientIp)))
                .ToListAsync();
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

        public Task<bool> HasNewerClient(int version)
        {
            return Task.FromResult(version < Utils.ClientVersion);
        }
    }
}
