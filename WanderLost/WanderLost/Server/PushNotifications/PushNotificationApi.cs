﻿
using Microsoft.AspNetCore.Mvc;
using WanderLost.Shared.Data;

namespace WanderLost.Server.PushNotifications;

[ApiController]
[Route("api/PushNotifications")]
public class PushNotificationsController(PushSubscriptionManager _pushSubscriptionManager) : ControllerBase
{
    [HttpPost]
    [Route(nameof(GetPushSubscription))]
    public async Task<ActionResult<PushSubscription?>> GetPushSubscription([FromBody] string clientToken)
    {
        if (string.IsNullOrWhiteSpace(clientToken)) return BadRequest();

        return await _pushSubscriptionManager.GetPushSubscription(clientToken);
    }

    [HttpPost]
    [Route(nameof(UpdatePushSubscription))]
    public async Task<StatusCodeResult> UpdatePushSubscription([FromBody] PushSubscription subscription)
    {
        if (!await _pushSubscriptionManager.ValidatePushSubscription(subscription)) return BadRequest();

        await _pushSubscriptionManager.UpdatePushSubscription(subscription);

        return Ok();
    }

    [HttpPost]
    [Route(nameof(RemovePushSubscription))]
    public async Task<StatusCodeResult> RemovePushSubscription([FromBody] string clientToken)
    {
        if (string.IsNullOrWhiteSpace(clientToken)) return BadRequest();

        await _pushSubscriptionManager.RemovePushSubscription(clientToken);

        return Ok();
    }
}
