
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
        _merchantsDbContext.SaveChanges();

        return Ok();
    }

    [HttpPost]
    [Route(nameof(RemovePushSubscription))]
    public async Task<StatusCodeResult> RemovePushSubscription([FromBody] string clientToken)
    {
        if (string.IsNullOrEmpty(clientToken)) return BadRequest();

        try
        {
            var subscription = new PushSubscription()
            {
                Token = clientToken,
            };
            //Rather than delete, just purge all data from the record by storing blank values
            //If we delete, then this occasionally causes a race condition for primary/foreign key updates
            //in the background processors when pushing out notifications
            _merchantsDbContext.Entry(subscription).State = EntityState.Modified;
            await _merchantsDbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            //If a subscription didn't exist, just ignore the error.
            //Probably happens mainly if a user multi-clicks delete before the request has completed
        }

        return Ok();
    }
}
