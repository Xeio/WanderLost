using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Authentication;
using WanderLost.Shared.Data;
using WanderLost.Shared.Interfaces;

namespace WanderLost.Server.Controllers;

public class MerchantHub : Hub<IMerchantHubClient>, IMerchantHubServer
{
    public static string Path { get; } = "MerchantHub";

    private readonly DataController _dataController;
    private readonly MerchantsDbContext _merchantsDbContext;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;

    public MerchantHub(DataController dataController, MerchantsDbContext merchantsDbContext, IConfiguration configuration, IMemoryCache memoryCache)
    {
        _dataController = dataController;
        _merchantsDbContext = merchantsDbContext;
        _configuration = configuration;
        _memoryCache = memoryCache;
    }

    [Authorize(Policy = nameof(RareCombinationRestricted))]
    public async Task UpdateMerchant(string server, ActiveMerchant merchant)
    {
        if (merchant is null) return;

        if (!await IsValidServer(server)) return;

        //Compatability shim
        if (merchant.Rapport.Name == "Surprise Chest") merchant.Rapport = new Item() { Name = "Pit-a-Pat Chest", Rarity = Rarity.Legendary };

        var allMerchantData = await _dataController.GetMerchantData();
        if (!merchant.IsValid(allMerchantData)) return;

        if(!await _dataController.IsServerOnline(server)) return;

        var merchantGroup = await _merchantsDbContext.MerchantGroups
            .TagWithCallSite()
            .Where(g => g.Server == server && g.MerchantName == merchant.Name && g.AppearanceExpires > DateTimeOffset.Now)
            .Include(g => g.ActiveMerchants)
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

            _merchantsDbContext.MerchantGroups.Add(merchantGroup);
        }

        var clientIp = GetClientIp();

        //If a client already uploaded a merchant, ignore further uploads and just skip out
        if (merchantGroup.ActiveMerchants.Any(m => m.UploadedBy == clientIp || (Context.UserIdentifier != null && m.UploadedByUserId == Context.UserIdentifier))) return;

        foreach (var existingMerchant in merchantGroup.ActiveMerchants)
        {
            if (existingMerchant.IsEqualTo(merchant))
            {
                if (existingMerchant.Hidden)
                {
                    existingMerchant.Hidden = false;
                    await _merchantsDbContext.SaveChangesAsync();

                    //Need to update the clients since we un-hid an item, also remove any other possible hidden items
                    merchantGroup.ActiveMerchants.RemoveAll(m => m.Hidden);

                    await Clients.Group(server).UpdateMerchantGroup(server, merchantGroup);
                }

                //Vote method attaches the merchant entity without database fetch to set the vote process flag
                //Before calling it clear out the change tracker so we don't get any duplicate entity exceptions
                _merchantsDbContext.ChangeTracker.Clear();
                //Found an existing matching merchant, so just upvote it instead
                await Vote(server, existingMerchant.Id, VoteType.Upvote);
                return;
            }
        }

        //Special handling case for banned users
        if (await HandleBans(clientIp, server, merchantGroup, merchant)) return;

        //If this client is uploading to multiple servers, ignore them
        if (await ClientHasOtherServerUploads(server, clientIp, Context.UserIdentifier)) return;

        merchant.UploadedBy = clientIp;
        merchant.UploadedByUserId = Context.UserIdentifier;
        merchant.RequiresProcessing = true;
        //Add an auto-upvote so the user can see their own submissions by default
        merchant.ClientVotes.Add(new Vote() { ClientId = clientIp, UserId = Context.UserIdentifier, VoteType = VoteType.Upvote });
        merchantGroup.ActiveMerchants.Add(merchant);

        await _merchantsDbContext.SaveChangesAsync();

        //Before we send to clients, remove anything that should be hidden
        merchantGroup.ActiveMerchants.RemoveAll(m => m.Hidden);

        await Clients.Group(server).UpdateMerchantGroup(server, merchantGroup);
    }

    private async Task<bool> HandleBans(string clientIp, string server, ActiveMerchantGroup group, ActiveMerchant merchant)
    {            
        //Skip out if no bans
        if (!await HasActiveBan(clientIp, Context.UserIdentifier)) return false;

        merchant.UploadedBy = clientIp;
        merchant.UploadedByUserId = Context.UserIdentifier;
        merchant.Hidden = true;
        //Add an auto-upvote so the user can see their own submissions by default
        merchant.ClientVotes.Add(new Vote() { ClientId = clientIp, UserId = Context.UserIdentifier, VoteType = VoteType.Upvote });

        group.ActiveMerchants.Add(merchant);

        await _merchantsDbContext.SaveChangesAsync();

        await Clients.Caller.UpdateMerchantGroup(server, group);

        return true;
    }

    private async Task<bool> ClientHasOtherServerUploads(string originalServer, string clientId, string? userId)
    {
        return await _merchantsDbContext.MerchantGroups
            .TagWithCallSite()
            .Where(g => g.Server != originalServer && g.AppearanceExpires > DateTimeOffset.Now)
            .AnyAsync(g => g.ActiveMerchants.Any(m => m.UploadedBy == clientId || (userId != null && m.UploadedByUserId == userId)));
    }

    public async Task Vote(string server, Guid merchantId, VoteType voteType)
    {
        var clientId = GetClientIp();

        Vote? existingVote;
        if (!string.IsNullOrWhiteSpace(Context.UserIdentifier))
        {
            existingVote = await _merchantsDbContext.Votes
               .TagWithCallSite()
               .Where(v => v.ActiveMerchantId == merchantId)
               .FirstOrDefaultAsync(v => v.UserId == Context.UserIdentifier);
        }
        else
        {
            existingVote = await _merchantsDbContext.Votes
               .TagWithCallSite()
               .Where(v => v.ActiveMerchantId == merchantId)
               .FirstOrDefaultAsync(v => v.ClientId == clientId);
        }
        if(existingVote is null)
        {
            var vote = new Vote()
            {
                ActiveMerchantId = merchantId,
                ClientId = clientId,
                UserId = Context.UserIdentifier,
                VoteType = voteType,
            };

            _merchantsDbContext.Votes.Add(vote);

            SetVoteProcessFlag(merchantId);

            await _merchantsDbContext.SaveChangesAsync();

            //Vote totals are tallied and sent by BackgroundVoteProcessor, just tell client their vote was counted
            await Clients.Caller.UpdateVoteSelf(merchantId, voteType);
        }
        else if(existingVote.VoteType != voteType)
        {
            existingVote.VoteType = voteType;

            SetVoteProcessFlag(merchantId);

            await _merchantsDbContext.SaveChangesAsync();

            //Vote totals are tallied and sent by BackgroundVoteProcessor, just tell client their vote was counted
            await Clients.Caller.UpdateVoteSelf(merchantId, voteType);
        }
    }

    private void SetVoteProcessFlag(Guid merchantId)
    {
        var updateMerchant = new ActiveMerchant()
        {
            Id = merchantId,
            RequiresVoteProcessing = true
        };
        _merchantsDbContext.Entry(updateMerchant).Property(m => m.RequiresVoteProcessing).IsModified = true;
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
            .TagWithCallSite()
            .Where(g => g.Server == server && g.AppearanceExpires > DateTimeOffset.Now)
            .Select(mg => new ActiveMerchantGroup
            {
                 Server = mg.Server,
                 MerchantName = mg.MerchantName,
                 ActiveMerchants = mg.ActiveMerchants
                                        .Where(m => !m.Hidden || (clientIp != null && m.UploadedBy == clientIp) || (Context.UserIdentifier != null && m.UploadedByUserId == Context.UserIdentifier))
                                        .ToList(),
            })
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Vote>> RequestClientVotes(string server)
    {
        if (!string.IsNullOrWhiteSpace(Context.UserIdentifier))
        {
            return await _merchantsDbContext.MerchantGroups
            .TagWithCallSite()
            .AsNoTracking()
            .Where(g => g.Server == server && g.AppearanceExpires > DateTimeOffset.Now)
            .SelectMany(mg => mg.ActiveMerchants.SelectMany(m => m.ClientVotes))
            .Where(vote => vote.UserId == Context.UserIdentifier)
            .ToListAsync();
        }
        var clientIp = GetClientIp();
        return await _merchantsDbContext.MerchantGroups
            .TagWithCallSite()
            .AsNoTracking()
            .Where(g => g.Server == server && g.AppearanceExpires > DateTimeOffset.Now)
            .SelectMany(mg => mg.ActiveMerchants.SelectMany(m => m.ClientVotes))
            .Where(vote => vote.ClientId == clientIp)
            .ToListAsync();
    }

    private string GetClientIp()
    {
#if DEBUG
        //In debug mode, allow using the connection ID to simulate multiple clients
        return Context.ConnectionId;
#else

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
#endif
    }

    public Task<bool> HasNewerClient(int version)
    {
        if (int.TryParse(_configuration["ClientVersion"], out var currentVersion))
        {
            return Task.FromResult(version < currentVersion);
        }
        //If the config is missing for some reason default to false
        return Task.FromResult(false);
    }
    private async Task<bool> HasActiveBan(string clientId, string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await _merchantsDbContext.Users
                .TagWithCallSite()
                .FirstOrDefaultAsync(u => u.Id == userId);
            if(user is not null)
            {
                return user.BanExpires > DateTimeOffset.Now;
            }
        }

        return await _merchantsDbContext.Bans
            .TagWithCallSite()
            .AnyAsync(b => b.ClientId == clientId && b.ExpiresAt > DateTimeOffset.Now);
    }

    private async Task<int> UserVoteTotal(string userId)
    {
        return await _merchantsDbContext.ActiveMerchants.TagWithCallSite().Where(m => m.UploadedByUserId == userId).SumAsync(m => m.Votes);
    }

    public async Task<PushSubscription?> GetPushSubscription(string clientToken)
    {
        if (string.IsNullOrEmpty(clientToken)) return null;

        return await _merchantsDbContext.PushSubscriptions
            .TagWithCallSite()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Token == clientToken);
    }

    public async Task UpdatePushSubscription(PushSubscription subscription)
    {
        if (string.IsNullOrEmpty(subscription.Token)) return;

        int existingSubscriptionId = await _merchantsDbContext.PushSubscriptions
                        .TagWithCallSite()
                        .Where(s => s.Token == subscription.Token)
                        .Select(s => s.Id)
                        .FirstOrDefaultAsync();

        if (existingSubscriptionId > 0)
        {
            subscription.Id = existingSubscriptionId;
            _merchantsDbContext.Entry(subscription).State = EntityState.Modified;
        }
        else
        {
            _merchantsDbContext.Add(subscription);
        }

        await _merchantsDbContext.SaveChangesAsync();
    }

    public async Task RemovePushSubscription(string clientToken)
    {
        if (string.IsNullOrEmpty(clientToken)) return;

        //Rather than delete, just purge the server data
        //If we delete, then this occasionally causes a race condition for primary/foreign key updates
        //in the background processors when pushing out notifications
        //These orphaned subscriptions will be cleaned up by the PurgeProcessor periodically
        await _merchantsDbContext.PushSubscriptions
            .TagWithCallSite()
            .Where(s => s.Token == clientToken)
            .ExecuteUpdateAsync(s => 
                s.SetProperty(i => i.Server, i => string.Empty)
                 .SetProperty(i => i.LastModified, i => DateTimeOffset.UtcNow)
            );
    }

    [Authorize]
    public async Task<ProfileStats> GetProfileStats()
    {
        var leaderboardEntry = await _merchantsDbContext.Leaderboards
            .TagWithCallSite()
            .Where(l => l.UserId == Context.UserIdentifier)
            .FirstOrDefaultAsync();

        return new ProfileStats()
        {
            PrimaryServer = leaderboardEntry?.PrimaryServer ?? "No submissions",
            TotalUpvotes = leaderboardEntry?.TotalVotes ?? 0,
            UpvotedMerchats = leaderboardEntry?.TotalSubmissions ?? 0,
            DisplayName = leaderboardEntry?.DisplayName,
            //NewestSubmission = votesAndCount?.NewestSubmission != null ? DateOnly.FromDateTime(votesAndCount.NewestSubmission) : null,
            //OldestSubmission = votesAndCount?.OldestSubmission != null ? DateOnly.FromDateTime(votesAndCount.OldestSubmission) : null,
        };
    }

    public async Task<WeiStats> GetWeiStats()
    {
        return await _memoryCache.GetOrCreateAsync(nameof(GetWeiStats), async (cacheEntry) =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            await _merchantsDbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadUncommitted);

            WeiStats stats = new();

            var weiCounts = await _merchantsDbContext.ActiveMerchants
                .TagWithCallSite()
                .Where(m => m.Card.Name == "Wei" && !m.Hidden && m.Votes > 0)
                .GroupBy(m => m.ActiveMerchantGroup.Server, (server, rows) => new
                {
                    Server = server,
                    Count = rows.Count()
                })
                .OrderByDescending(i => i.Count)
                .ToListAsync();

            var recentWeis = await _merchantsDbContext.ActiveMerchants
                .TagWithCallSite()
                .Where(m => m.Card.Name == "Wei" && !m.Hidden && m.Votes > 0)
                .OrderByDescending(m => m.ActiveMerchantGroup.NextAppearance)
                .Select(m => new { m.ActiveMerchantGroup.Server, m.ActiveMerchantGroup.NextAppearance })
                .Take(50)
                .ToListAsync();

            await _merchantsDbContext.Database.RollbackTransactionAsync();

            return new WeiStats()
            {
                ServerWeiCounts = weiCounts.Select(c => (c.Server, c.Count)).ToList(),
                RecentWeis = recentWeis.Select(r => (r.Server, r.NextAppearance)).ToList()
            };
        }) ?? new();
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboard(string? server)
    {
        if (string.IsNullOrWhiteSpace(server))
        {
            return await _merchantsDbContext.Leaderboards
                .TagWithCallSite()
                .OrderByDescending(m => m.TotalSubmissions)
                .Take(50)
                .ToListAsync();
        }
        return await _merchantsDbContext.Leaderboards
            .TagWithCallSite()
            .Where(l => l.PrimaryServer == server)
            .OrderByDescending(m => m.TotalSubmissions)
            .Take(50)
            .ToListAsync();
    }

    [Authorize]
    public async Task UpdateDisplayName(string? displayName)
    {
        if(Context.UserIdentifier is null) throw new AuthenticationException();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Anonymous";
        }

        var updateCount = await _merchantsDbContext.Leaderboards
            .Where(l => l.UserId == Context.UserIdentifier)
            .ExecuteUpdateAsync(u => u.SetProperty(l => l.DisplayName, l => displayName));

        if (updateCount == 0)
        {
            //Need to insert the empty leaderboard record instead
            _merchantsDbContext.Add(new LeaderboardEntry()
            {
                UserId = Context.UserIdentifier,
                DisplayName = displayName,
            });
            await _merchantsDbContext.SaveChangesAsync();
        }
    }
}
