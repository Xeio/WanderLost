using Microsoft.EntityFrameworkCore;
using WanderLost.Shared.Data;

namespace WanderLost.Server.Controllers;

/// <summary>
/// Processes leaderboard stats after merchants expire
/// </summary>
public class LeaderboardProcessor : BackgroundService
{
    private readonly IServiceProvider _services;

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
                .FirstOrDefaultAsync(stoppingToken);

            var server = await merchantDbContext.ActiveMerchants
                .TagWithCallSite()
                .Where(m => m.UploadedByUserId == merchant.UploadedByUserId && m.Votes >= 0 && !m.Hidden)
                .Include(m => m.ActiveMerchantGroup)
                .GroupBy(m => m.ActiveMerchantGroup.Server, (server, rows) => new {
                    Server = server,
                    Count = rows.Count()
                })
                .OrderByDescending(i => i.Count)
                .Select(i => i.Server)
                .FirstOrDefaultAsync(stoppingToken);

            //TODO: Use bulk updates in EF 7 branch
            var updateCount = await merchantDbContext.Database.ExecuteSqlInterpolatedAsync(@$"
UPDATE Leaderboards
SET TotalVotes = {votesAndCount?.VoteTotal ?? 0},
    TotalSubmissions = {votesAndCount?.TotalSubmisisons ?? 0},    
    PrimaryServer = {server}
WHERE UserId = {merchant.UploadedByUserId}
",  stoppingToken);

            if(updateCount == 0)
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

            //TODO: Use bulk updates in EF 7 branch
            //Can clear processing flag for all rows with this user since all rows are aggregated at once
            await merchantDbContext.Database.ExecuteSqlInterpolatedAsync(@$"
UPDATE ActiveMerchants
SET RequiresLeaderboardProcessing = 0
WHERE UploadedByUserId = {merchant.UploadedByUserId}
",  stoppingToken);
        }
    }
}
