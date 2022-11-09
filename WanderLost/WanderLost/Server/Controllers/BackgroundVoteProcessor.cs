using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WanderLost.Shared.Data;
using WanderLost.Shared.Interfaces;

namespace WanderLost.Server.Controllers;

public class BackgroundVoteProcessor : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PushWorkerService> _logger;
    private readonly List<string> _servers = new();

    public BackgroundVoteProcessor(ILogger<PushWorkerService> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using(var startupScope = _services.CreateScope())
        {
            //Get all the servers, which we'll process one at a time
            var dataController = startupScope.ServiceProvider.GetRequiredService<DataController>();
            var regions = await dataController.GetServerRegions();
            _servers.AddRange(regions.SelectMany(r => r.Value.Servers));

        }

        if(_servers.Count == 0)
        {
            _logger.LogCritical("Didn't find any servers to poll.");
            return;
        }

        //Going to target each server to update every 30 seconds, so pause roughly equivalent between them
        //Note that servers aren't equally distributed, so may be better to randomize order? Or one at a time from each data center?
        int perServerMillisecondDelay = 30_000 / _servers.Count;

        while (true)
        {
            foreach (var server in _servers)
            {
                await Task.Delay(perServerMillisecondDelay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) return;

                using var scope = _services.CreateScope();
                var merchantDbContext = scope.ServiceProvider.GetRequiredService<MerchantsDbContext>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<MerchantHub, IMerchantHubClient>>();

                var merchantsToProcess = await merchantDbContext.ActiveMerchants
                    .TagWithCallSite()
                    .Where(m => m.RequiresVoteProcessing)
                    .Where(m => m.ActiveMerchantGroup.Server == server)
                    .Include(m => m.ClientVotes)
                    .Include(m => m.ActiveMerchantGroup)
                    .ToListAsync(stoppingToken);

                var voteUpdates = new List<MerchantVoteUpdate>();

                foreach (var merchant in merchantsToProcess)
                {
                    if (stoppingToken.IsCancellationRequested) return;

                    if (merchant.ActiveMerchantGroup.AppearanceExpires > DateTime.UtcNow)
                    {
                        var oldTotal = merchant.Votes;
                        merchant.Votes = CalculateVoteTotal(merchant);
                        if (merchant.Votes != oldTotal)
                        {
                            merchant.RequiresProcessing = true;
                            voteUpdates.Add(new MerchantVoteUpdate()
                            {
                                Id = merchant.Id,
                                Votes = merchant.Votes,
                            });
                        }
                    }

                    merchant.RequiresVoteProcessing = false;
                }

                await merchantDbContext.SaveChangesAsync(stoppingToken);

                if (voteUpdates.Count > 0)
                {
                    await hubContext.Clients.Group(server).UpdateVotes(voteUpdates);
                }
            }
        }
    }

    private static int CalculateVoteTotal(ActiveMerchant merchant)
    {
        //Small special case here, we won't count the submitter's vote, but track it so they can see they already "voted"
        return merchant.ClientVotes
            .Where(v => v.ClientId != merchant.UploadedBy && (merchant.UploadedByUserId == null || v.UserId != merchant.UploadedByUserId))
            .Sum(v => (int)v.VoteType);
    }
}
