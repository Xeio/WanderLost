using Microsoft.EntityFrameworkCore;

namespace WanderLost.Server.Controllers;

/// <summary>
/// Handles cleanup of old data to constrain growth of database
/// </summary>
public class PurgeProcessor(ILogger<PurgeProcessor> _logger, IServiceScopeFactory _scopeFactory) : BackgroundService
{
    const int DAYS_TO_KEEP = 7;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(70), stoppingToken);

            if (stoppingToken.IsCancellationRequested) return;

            using var scope = _scopeFactory.CreateScope();
            var merchantDbContext = scope.ServiceProvider.GetRequiredService<MerchantsDbContext>();

            merchantDbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            var oldMerchants = merchantDbContext.ActiveMerchants
                .Where(am => am.ActiveMerchantGroup.AppearanceExpires < DateTime.UtcNow.AddDays(-DAYS_TO_KEEP));

            //Cleanup votes table (totals will still be preserved)
            var deletedVotes = await merchantDbContext.Votes
                .TagWithCallSite()
                .Where(v => oldMerchants.Any(m => m.Id == v.ActiveMerchantId))
                .ExecuteDeleteAsync(stoppingToken);

            //Cleanup sent FCM push notifications
            var deletedPushes = await merchantDbContext.SentPushNotifications
                .TagWithCallSite()
                .Where(p => oldMerchants.Any(m => m.Id == p.MerchantId))
                .ExecuteDeleteAsync(stoppingToken);

            //Cleanup sent discord push notifications
            deletedPushes += await merchantDbContext.SentDiscordNotifications
                .TagWithCallSite()
                .Where(p => oldMerchants.Any(m => m.Id == p.MerchantId))
                .ExecuteDeleteAsync(stoppingToken);

            //Purge FCM subscriptions that aren't actually subscribed to any notifications
            var deletedSubscriptions = await merchantDbContext.PushSubscriptions
                .TagWithCallSite()
                .Where(s => string.IsNullOrEmpty(s.Server) || (!s.SendTestNotification && s.CardNotifications.Count == 0 && !s.LegendaryRapportNotify))
                .ExecuteDeleteAsync(stoppingToken);

            //Purge discord subscriptions that aren't actually subscribed to any notifications
            deletedSubscriptions += await merchantDbContext.DiscordNotifications
                .TagWithCallSite()
                .Where(s => string.IsNullOrEmpty(s.Server) || (!s.SendTestNotification && s.CardNotifications.Count == 0))
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("Purged {votes} votes, {pushes} sent push notifications, and {subscriptions} empty subscriptions.", deletedVotes, deletedPushes, deletedSubscriptions);
        }
    }
}
