using Microsoft.EntityFrameworkCore;
using WanderLost.Shared.Data;

namespace WanderLost.Server.Controllers;

/// <summary>
/// Processes leaderboard stats after merchants expire
/// </summary>
public class LeaderboardProcessor : BackgroundService
{
    private readonly IServiceProvider _services;
    private string[]? _validServers;

    public LeaderboardProcessor(IServiceProvider services)
    {
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            if (stoppingToken.IsCancellationRequested) return;

            using var scope = _services.CreateScope();
            var merchantDbContext = scope.ServiceProvider.GetRequiredService<MerchantsDbContext>();

            var merchant = await merchantDbContext.ActiveMerchants
                .TagWithCallSite()
                .Where(m => m.RequiresLeaderboardProcessing && m.ActiveMerchantGroup.AppearanceExpires < DateTime.UtcNow)
                .FirstOrDefaultAsync(stoppingToken);

            if (merchant is null)
            {
                //No merchants to process, can sleep for a bit
                scope.Dispose();
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                continue;
            }

            if (string.IsNullOrWhiteSpace(merchant.UploadedByUserId))
            {
                //Don't need to process merchants without a user
                merchant.RequiresLeaderboardProcessing = false;
                await merchantDbContext.SaveChangesAsync(stoppingToken);
                continue;
            }

            var votesAndCount = await merchantDbContext.ActiveMerchants
                .TagWithCallSite()
                .Where(m => m.UploadedByUserId == merchant.UploadedByUserId && m.Votes >= 0 && !m.Hidden)
                .GroupBy(m => m.UploadedByUserId, (_, rows) => new
                {
                    VoteTotal = rows.Sum(m => m.Votes),
                    TotalSubmisisons = rows.Count(),
                    OldestSubmission = rows.Max(m => m.ActiveMerchantGroup.NextAppearance.Date),
                    NewestSubmission = rows.Max(m => m.ActiveMerchantGroup.NextAppearance.Date),
                })
                .FirstOrDefaultAsync(stoppingToken) ?? new { VoteTotal = 0, TotalSubmisisons = 0, OldestSubmission = DateTime.Now, NewestSubmission = DateTime.Now };

            if (_validServers is null)
            {
                var staticData = scope.ServiceProvider.GetRequiredService<DataController>();
                _validServers = (await staticData.GetServerRegions()).SelectMany(sr => sr.Value.Servers).ToArray();
            }

            var server = await merchantDbContext.ActiveMerchants
                .TagWithCallSite()
                .Where(m => _validServers.Contains(m.ActiveMerchantGroup.Server))
                .Where(m => m.UploadedByUserId == merchant.UploadedByUserId && m.Votes >= 0 && !m.Hidden)
                .Include(m => m.ActiveMerchantGroup)
                .GroupBy(m => m.ActiveMerchantGroup.Server, (server, rows) => new
                {
                    Server = server,
                    Count = rows.Count()
                })
                .OrderByDescending(i => i.Count)
                .Select(i => i.Server)
                .FirstOrDefaultAsync(stoppingToken) ?? string.Empty;

            var updateCount = await merchantDbContext.Leaderboards
                .Where(l => l.UserId == merchant.UploadedByUserId)
                .ExecuteUpdateAsync(u => u
                                .SetProperty(l => l.TotalVotes, l => votesAndCount.VoteTotal)
                                .SetProperty(l => l.TotalSubmissions, l => votesAndCount.TotalSubmisisons)
                                .SetProperty(l => l.PrimaryServer, l => server),
                                stoppingToken);

            if (updateCount == 0)
            {
                //Need to insert the leaderboard record instead
                merchantDbContext.Add(new LeaderboardEntry()
                {
                    UserId = merchant.UploadedByUserId,
                    TotalVotes = votesAndCount?.VoteTotal ?? 0,
                    TotalSubmissions = votesAndCount?.TotalSubmisisons ?? 0,
                    DisplayName = "Anonymous",
                    PrimaryServer = server ?? string.Empty,
                });
                await merchantDbContext.SaveChangesAsync(stoppingToken);
            }

            //Can clear processing flag for all rows with this user since all rows are aggregated at once
            await merchantDbContext.ActiveMerchants
                .Where(m => m.UploadedByUserId == merchant.UploadedByUserId)
                .ExecuteUpdateAsync(u => u.SetProperty(m => m.RequiresLeaderboardProcessing, m => false));
        }
    }
}
