using Microsoft.JSInterop;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Services;

public sealed class ClientNotificationService : IAsyncDisposable
{
    private readonly ClientSettingsController _clientSettings;
    private readonly IJSRuntime _jsRuntime;
    private readonly List<IJSObjectReference> _notifications = new();

    private Dictionary<string, DateTimeOffset> _merchantFoundNotificationCooldown = new();
    public ClientNotificationService(ClientSettingsController clientSettings, IJSRuntime js)
    {
        _clientSettings = clientSettings;
        _jsRuntime = js;
    }

    public async Task Init()
    {
        await _clientSettings.Init();
    }

    /// <summary>
    /// Check if browser supports notifications.
    /// </summary>
    /// <returns></returns>
    public ValueTask<bool> IsSupportedByBrowser()
    {
        try
        {
            return _jsRuntime.InvokeAsync<bool>("SupportsNotifications");
        }
        catch
        {
            return ValueTask.FromResult(false);
        }
    }

    /// <summary>
    /// Request permission to send notifications from user.
    /// </summary>
    /// <returns></returns>
    public async ValueTask<bool> RequestPermission()
    {
        try
        {
            var permissionResult = await _jsRuntime.InvokeAsync<string>("RequestPermission");
            return permissionResult == "granted";
        }
        catch
        {
            return false;
        }
    }

    public ValueTask<string?> GetFCMToken()
    {
        return _jsRuntime.InvokeAsync<string?>("GetServiceWorkerToken");
    }

    private bool IsMerchantCardVoteThresholdReached(ActiveMerchant merchant)
    {
        _clientSettings.CardVoteThresholdForNotification.TryGetValue(merchant.Card.Rarity, out int voteThreshold); //if TryGetValue fails, voteThreshold will be 0, actual true/false result does no matter in this case
        return merchant.Votes >= voteThreshold;
    }

    private bool IsMerchantRapportVoteThresholdReached(ActiveMerchant merchant)
    {
        _clientSettings.RapportVoteThresholdForNotification.TryGetValue(merchant.Rapport.Rarity, out int voteThreshold); //if TryGetValue fails, voteThreshold will be 0, actual true/false result does no matter in this case
        return merchant.Votes >= voteThreshold;
    }

    private bool IsMerchantFoundNotificationOnCooldown(ActiveMerchantGroup merchantGroup)
    {
        return _merchantFoundNotificationCooldown.TryGetValue(merchantGroup.MerchantName, out DateTimeOffset cooldown) && DateTime.Now < cooldown;
    }

    private bool IsAllowedForMerchantFoundNotifications(ActiveMerchantGroup merchantGroup)
    {
        if (merchantGroup.ActiveMerchants.Count == 0) return false;

        if (!_clientSettings.Notifications.TryGetValue(merchantGroup.MerchantName, out var notificationSetting))
        {
            return false;
        }

        if (IsMerchantFoundNotificationOnCooldown(merchantGroup))
        {
            return false;
        }

        //check cards
        foreach (var merchant in merchantGroup.ActiveMerchants.Where(m => notificationSetting.Cards.Contains(m.Card.Name)))
        {
            if (IsMerchantCardVoteThresholdReached(merchant))
            {
                return true;
            }
        }
        //check rapports
        foreach (var merchant in merchantGroup.ActiveMerchants.Where(m => notificationSetting.Rapports.Contains(m.Rapport.Name)))
        {
            if (IsMerchantRapportVoteThresholdReached(merchant))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Request a "merchant found" Browser-Notification for the given merchantGroup, rules from usersettings are applied; the request can be denied.
    /// </summary>
    /// <param name="merchantGroup"></param>
    /// <returns></returns>
    public async ValueTask CheckItemNotification(ActiveMerchantGroup merchantGroup)
    {
        if (!_clientSettings.NotificationsEnabled) return;
        if (!IsAllowedForMerchantFoundNotifications(merchantGroup)) return;

        _merchantFoundNotificationCooldown[merchantGroup.MerchantName] = merchantGroup.AppearanceExpires;

        await CheckBrowserNotificationSound(merchantGroup);
        await CheckBrowserNotification(merchantGroup);
    }

    /// <summary>
    /// Force a "merchant found" Browser-Notification for the given merchantGroup, rules from usersettings are NOT applied.
    /// </summary>
    /// <param name="merchantGroup"></param>
    /// <returns></returns>
    public async ValueTask CheckBrowserNotification(ActiveMerchantGroup merchantGroup)
    {
        if (!_clientSettings.BrowserNotifications) return;

        string body = "";
        if (merchantGroup.ActiveMerchants.Count > 1)
        {
            body += "Conflicting merchant data, click for more information.";
        }
        else
        {
            body += $"Location: {merchantGroup.ActiveMerchants[0].Zone}\n";
            body += $"Card: {merchantGroup.ActiveMerchants[0].Card.Name}\n";
            body += $"Rapport: {merchantGroup.ActiveMerchants[0].Rapport.Name}\n";
        }

        await CreateNotification($"Wandering Merchant \"{merchantGroup.MerchantName}\" found", new { Body = body, Renotify = true, Tag = $"found_{merchantGroup.MerchantName}", Icon = "images/notifications/ExclamationMark.png" });
    }

    /// <summary>
    /// Request a "merchant appeared" Browser-Notification for the given merchantGroup, rules from usersettings are applied; the request can be denied.
    /// </summary>
    /// <param name="merchantGroup"></param>
    /// <returns></returns>
    public ValueTask CheckMerchantSpawnNotification(IEnumerable<ActiveMerchantGroup> merchantGroups)
    {
        if (!_clientSettings.NotificationsEnabled) return ValueTask.CompletedTask;

        foreach (var merchantGroup in merchantGroups)
        {
            //Check if spawn notification is enabled
            if (!_clientSettings.Notifications.TryGetValue(merchantGroup.MerchantName, out var notificationSetting) || !notificationSetting.NotifySpawn) continue;

            //Only showing the first enabled "spawn" notification, then returning
            string body = $"Wandering Merchant \"{merchantGroup.MerchantName}\" is waiting for you somewhere.";
            return CreateNotification($"Wandering Merchant \"{merchantGroup.MerchantName}\" appeared", new { Body = body, Renotify = true, Tag = "spawn_merchant", Icon = "images/notifications/QuestionMark.png" });
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Force a "merchant appeared" Browser-Notification for the given merchantGroup, rules from usersettings are NOT applied.
    /// </summary>
    /// <param name="merchantGroup"></param>
    /// <returns></returns>
    public async ValueTask ForceMerchantSpawnNotification(ActiveMerchantGroup merchantGroup)
    {
        await CheckBrowserNotificationSound(merchantGroup, true);

        string body = $"Wandering Merchant \"{merchantGroup.MerchantName}\" is waiting for you somewhere.";
        await CreateNotification($"Wandering Merchant \"{merchantGroup.MerchantName}\" appeared", new { Body = body, Renotify = true, Tag = "spawn_merchant", Icon = "images/notifications/QuestionMark.png" });
    }

    private async ValueTask CreateNotification(string title, object parameters)
    {
        var notification = await _jsRuntime.InvokeAsync<IJSObjectReference>("Create", title, parameters);
        _notifications.Add(notification);
    }

    public async ValueTask CheckBrowserNotificationSound(ActiveMerchantGroup merchantGroup, bool force = false)
    {
        if (!force)
        {
            if (!_clientSettings.NotifyBrowserSoundEnabled) return;

            if (_clientSettings.RareSoundsOnly && !merchantGroup.ActiveMerchants.Any(m => m.IsRareCombination)) return;
        }

        try
        {
            await _jsRuntime.InvokeAsync<string>("PlayNotificationSound", _clientSettings.SoundVolume);
        }
        catch (Exception)
        {
            //ignore
            //if the sound doesn't play... whatever. No need to let the whole session crash.
        }
    }

    public async ValueTask ClearNotifications()
    {
        foreach(var notification in _notifications)
        {
            await _jsRuntime.InvokeVoidAsync("Dismiss", notification);
            await notification.DisposeAsync();
        }
        _notifications.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var notification in _notifications)
        {
            await notification.DisposeAsync();
        }
        _notifications.Clear();
    }

    /// <summary>
    /// Synchronize our subscription if our FCM token has changed
    /// </summary>
    public async Task ValidatePushSubscription(MerchantHubClient hubClient, string? currentToken = null)
    {
        if (_clientSettings.SavedPushSubscription?.Token == null) return;

        currentToken ??= await GetFCMToken();

        if (!string.IsNullOrWhiteSpace(currentToken) && 
            _clientSettings.SavedPushSubscription.Token != currentToken)
        {
            //Our token changed since we last subscribed
            //Remove the old subscription, then use the new token to re-subscribe
            await hubClient.RemovePushSubscription(_clientSettings.SavedPushSubscription.Token);
            _clientSettings.SavedPushSubscription.Token = currentToken;
            await hubClient.UpdatePushSubscription(_clientSettings.SavedPushSubscription);
            await _clientSettings.SetSavedPushSubscription(_clientSettings.SavedPushSubscription);
        }
    }
}
