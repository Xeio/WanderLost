using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WanderLost.Shared.Data;
using WanderLost.Shared.Interfaces;

namespace WanderLost.Server.Controllers
{
    public class BackgroundVoteProcessor : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<PushWorkerService> _logger;

        public BackgroundVoteProcessor(ILogger<PushWorkerService> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                if (stoppingToken.IsCancellationRequested) return;

                using var scope = _services.CreateScope();
                var merchantDbContext = scope.ServiceProvider.GetRequiredService<MerchantsDbContext>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<MerchantHub, IMerchantHubClient>>();

                var merchantIdsToProcess = await merchantDbContext.ActiveMerchants
                    .TagWithCallSite()
                    .Where(m => m.RequiresVoteProcessing)
                    .Select(m => m.Id)
                    .ToListAsync(stoppingToken);

                foreach(var merchantId in merchantIdsToProcess)
                {
                    if (stoppingToken.IsCancellationRequested) return;

                    var merchant = await merchantDbContext.ActiveMerchants
                        .TagWithCallSite()
                        .Include(m => m.ClientVotes)
                        .Include(m => m.ActiveMerchantGroup)
                        .FirstAsync(m => m.Id == merchantId, stoppingToken);

                    var oldTotal = merchant.Votes;
                    merchant.Votes = CalculateVoteTotal(merchant);
                    if(merchant.Votes != oldTotal)
                    {
                        merchant.RequiresProcessing = true;
                        await hubContext.Clients.Group(merchant.ActiveMerchantGroup.Server).UpdateVoteTotal(merchant.Id, merchant.Votes);
                    }

                    merchant.RequiresVoteProcessing = false;

                    await merchantDbContext.SaveChangesAsync(stoppingToken);

                    merchantDbContext.Entry(merchant).State = EntityState.Detached;
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
}
