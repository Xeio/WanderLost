using Microsoft.EntityFrameworkCore;

namespace WanderLost.Server.Controllers;

/// <summary>
/// Handles cleanup of old data to constrain growth of database
/// </summary>
public class PurgeProcessor : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PurgeProcessor> _logger;

    const int DAYS_TO_KEEP = 14;

    public PurgeProcessor(ILogger<PurgeProcessor> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            if (stoppingToken.IsCancellationRequested) return;

            //Only want to run this when site isn't normally active, which is usually around the top of the hour
            if (DateTime.Now.Minute > 4) continue;

            using var scope = _services.CreateScope();
            var merchantDbContext = scope.ServiceProvider.GetRequiredService<MerchantsDbContext>();

            //Cleanup votes table (totals will still be preserved)
            var deletedVotes = await merchantDbContext.Database.ExecuteSqlInterpolatedAsync(@$"
DELETE FROM V
FROM Votes V WITH(NOLOCK)
LEFT JOIN ActiveMerchants AM WITH(NOLOCK) ON V.ActiveMerchantId = AM.Id
LEFT JOIN MerchantGroups MG WITH(NOLOCK) ON MG.Id = AM.ActiveMerchantGroupId
WHERE MG.AppearanceExpires < DATEADD(DAY, {-DAYS_TO_KEEP}, SYSDATETIMEOFFSET())
", stoppingToken);

            //Cleanup sent push notifications
            var deletedPushes = await merchantDbContext.Database.ExecuteSqlInterpolatedAsync(@$"
DELETE FROM P
FROM SentPushNotifications P WITH(NOLOCK)
LEFT JOIN ActiveMerchants AM WITH(NOLOCK) ON P.MerchantId = AM.Id
LEFT JOIN MerchantGroups MG WITH(NOLOCK) ON MG.Id = AM.ActiveMerchantGroupId
WHERE MG.AppearanceExpires < DATEADD(DAY, {-DAYS_TO_KEEP}, SYSDATETIMEOFFSET()) 
", stoppingToken);

            _logger.LogInformation("Purged {votes} votes and {pushes} sent push notifications.", deletedVotes, deletedPushes);
        }
    }
}
