using Microsoft.EntityFrameworkCore;
using WanderLost.Server.Controllers;
using WanderLost.Shared.Data;

namespace WanderLost.Server.PushNotifications;

public class PushSubscriptionManager(MerchantsDbContext _merchantsDbContext, DataController _dataController)
{
    public async Task<PushSubscription?> GetPushSubscription(string clientToken)
    {
        if (string.IsNullOrWhiteSpace(clientToken)) return null;

        return await _merchantsDbContext.PushSubscriptions
            .TagWithCallSite()
            .AsNoTracking()
            .Include(s => s.CardNotifications)
            .FirstOrDefaultAsync(s => s.Token == clientToken);
    }

    public async Task UpdatePushSubscription(PushSubscription subscription)
    {
        if (!await ValidatePushSubscription(subscription)) return;

        var existingSubscription = await _merchantsDbContext.PushSubscriptions
                        .TagWithCallSite()
                        .Where(s => s.Token == subscription.Token)
                        .Include(s => s.CardNotifications)
                        .FirstOrDefaultAsync();

#pragma warning disable CS0618 // Type or member is obsolete
        if (subscription.WeiNotify && !subscription.CardNotifications.Any(c => c.CardName == "Wei"))
        {
            //Compatability for old way to notify on Wei card
            subscription.CardNotifications.Add(new CardNotification() { CardName = "Wei" });
        }
#pragma warning restore CS0618 // Type or member is obsolete

        if (existingSubscription is not null)
        {
            existingSubscription.Server = subscription.Server;
            existingSubscription.CardVoteThreshold = subscription.CardVoteThreshold;
            existingSubscription.RapportVoteThreshold = subscription.RapportVoteThreshold;
            existingSubscription.LegendaryRapportNotify = subscription.LegendaryRapportNotify;
            existingSubscription.CatalystNotification = subscription.CatalystNotification;
            existingSubscription.SendTestNotification = subscription.SendTestNotification;
            existingSubscription.LastModified = DateTimeOffset.Now;
            existingSubscription.ConsecutiveFailures = 0;
            existingSubscription.CardNotifications.Clear();
            foreach (var cardNotify in subscription.CardNotifications)
            {
                existingSubscription.CardNotifications.Add(cardNotify);
            }
        }
        else
        {
            _merchantsDbContext.Add(subscription);
        }

        await _merchantsDbContext.SaveChangesAsync();
    }

    public async Task<bool> ValidatePushSubscription(PushSubscription subscription)
    {
        if (string.IsNullOrWhiteSpace(subscription.Token)) return false;

        var merchants = await _dataController.GetMerchantData();
        var cardNames = merchants.SelectMany(m => m.Value.Cards).Select(c => c.Name).ToHashSet();
        return subscription.CardNotifications.All(cn => cardNames.Contains(cn.CardName));
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
}