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

            merchantDbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            var oldMerchants = merchantDbContext.ActiveMerchants
                .Where(am => am.ActiveMerchantGroup.AppearanceExpires < DateTime.UtcNow.AddDays(-DAYS_TO_KEEP));

            //Cleanup votes table (totals will still be preserved)
            var deletedVotes = await merchantDbContext.Votes
                .Where(v => oldMerchants.Any(m => m.Id == v.ActiveMerchantId))
                .ExecuteDeleteAsync(stoppingToken);

            //Cleanup sent push notifications
            var deletedPushes = await merchantDbContext.SentPushNotifications
                .Where(p => oldMerchants.Any(m => m.Id == p.MerchantId))
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("Purged {votes} votes and {pushes} sent push notifications.", deletedVotes, deletedPushes);
        }
    }
}
