
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WanderLost.Shared.Data;

namespace WanderLost.Server.Controllers;

[ApiController]
[Route("api/PushNotifications")]
public class PushNotificationsController : ControllerBase
{
    private readonly MerchantsDbContext _merchantsDbContext;

    public PushNotificationsController(MerchantsDbContext merchantsDbContext)
    {
        _merchantsDbContext = merchantsDbContext;
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

        bool exists = await _merchantsDbContext.PushSubscriptions
                        .TagWithCallSite()
                        .AnyAsync(s => s.Token == subscription.Token);
        if (exists)
        {
            _merchantsDbContext.Entry(subscription).State = EntityState.Modified;
        }
        else
        {
            _merchantsDbContext.Add(subscription);
        }

        await _merchantsDbContext.SaveChangesAsync();

        return Ok();
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
