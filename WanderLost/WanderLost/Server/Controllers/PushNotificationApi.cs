
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WanderLost.Shared.Data;

namespace WanderLost.Server.Controllers;

[ApiController]
[Route("api/PushNotifications")]
public class PushNotificationsController : ControllerBase
{
    private readonly MerchantsDbContext _merchantsDbContext;
    private readonly DataController _dataController;

    public PushNotificationsController(MerchantsDbContext merchantsDbContext, DataController dataController)
    {
        _merchantsDbContext = merchantsDbContext;
        _dataController = dataController;
    }

    [HttpPost]
    [Route(nameof(GetPushSubscription))]
    public async Task<PushSubscription?> GetPushSubscription([FromBody] string clientToken)
    {
        if (string.IsNullOrEmpty(clientToken)) return null;

        return await _merchantsDbContext.PushSubscriptions
            .TagWithCallSite()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Token == clientToken);
    }

    [HttpPost]
    [Route(nameof(UpdatePushSubscription))]
    public async Task<StatusCodeResult> UpdatePushSubscription([FromBody] PushSubscription subscription)
    {
        if (string.IsNullOrEmpty(subscription.Token)) return BadRequest();
        if (!await ValidateCards(subscription)) return BadRequest();

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
            existingSubscription.CardVoteThreshold = subscription.CardVoteThreshold;
            existingSubscription.RapportVoteThreshold = subscription.RapportVoteThreshold;
            existingSubscription.LegendaryRapportNotify = subscription.LegendaryRapportNotify;
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

        return Ok();
    }

    private async Task<bool> ValidateCards(PushSubscription subscription)
    {
        var merchants = await _dataController.GetMerchantData();
        var cardNames = merchants.SelectMany(m => m.Value.Cards).Select(c => c.Name).ToHashSet();
        return subscription.CardNotifications.All(cn => cardNames.Contains(cn.CardName));
    }

    [HttpPost]
    [Route(nameof(RemovePushSubscription))]
    public async Task<StatusCodeResult> RemovePushSubscription([FromBody] string clientToken)
    {
        if (string.IsNullOrEmpty(clientToken)) return BadRequest();

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

        return Ok();
    }
}
