using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Services;

public sealed class ClientNotificationService : IAsyncDisposable
{
    private readonly ActiveDataController _activeData;
    private readonly ClientSettingsController _clientSettings;
    private readonly IJSRuntime _jsRuntime;
    private readonly List<IJSObjectReference> _notifications = new();
    private readonly Dictionary<string, DateTimeOffset> _merchantFoundNotificationCooldown = new();

    public ClientNotificationService(ClientSettingsController clientSettings, IJSRuntime js, ActiveDataController activeData)
    {
        _clientSettings = clientSettings;
        _jsRuntime = js;
        _activeData = activeData;
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
    
    private bool AnyMerchantNotified(ActiveMerchantGroup merchantGroup, [NotNullWhen(true)] out ActiveMerchant? notifiedMerchant)
    {
        notifiedMerchant = null;
        if (merchantGroup.ActiveMerchants.Count == 0) return false;

        if (!_clientSettings.Notifications.TryGetValue(merchantGroup.MerchantName, out var notificationSetting))
        {
            return false;
        }

        //check cards
        foreach (var merchant in merchantGroup.ActiveMerchants.Where(m => notificationSetting.Cards.Contains(m.Card.Name)))
        {
            if (IsMerchantCardVoteThresholdReached(merchant))
            {
                notifiedMerchant = merchant;
                return true;
            }
        }
        //check rapports
        foreach (var merchant in merchantGroup.ActiveMerchants.Where(m => notificationSetting.Rapports.Contains(m.Rapport.Name)))
        {
            if (IsMerchantRapportVoteThresholdReached(merchant))
            {
                notifiedMerchant = merchant;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if any of the merchants in the group have a notified item and notify if not on cooldown.
    /// </summary>
    public async ValueTask CheckItemNotification(ActiveMerchantGroup merchantGroup)
    {
        if (!_clientSettings.NotificationsEnabled) return;

        if (IsMerchantFoundNotificationOnCooldown(merchantGroup)) return;

        if (!AnyMerchantNotified(merchantGroup, out var notifiedMerchant)) return;

        _merchantFoundNotificationCooldown[merchantGroup.MerchantName] = merchantGroup.AppearanceExpires;

        if(_activeData.Votes.ContainsKey(notifiedMerchant.Id))
        {
            //Don't need to play alert for merchants user has upvoted (either submitted or otherwise)
            return;
        }

        if (_clientSettings.NotifyBrowserSoundEnabled)
        {
            if (!_clientSettings.RareSoundsOnly || notifiedMerchant.IsRareCombination)
            {
                await PlaySound();
            }
        }

        if (_clientSettings.BrowserNotifications)
        {
            await BuildBroswerNotification(merchantGroup);
        }
    }

    public async ValueTask BuildBroswerNotification(ActiveMerchantGroup merchantGroup)
    {
        string body = "";
        var nonNegativeMerchants = merchantGroup.ActiveMerchants.Where(m => m.Votes >= 0).ToList();
        if (nonNegativeMerchants.Count > 1)
        {
            body += "Conflicting merchant data, click for more information.";
        }
        else if(nonNegativeMerchants.Count > 0)
        {
            body += $"Location: {nonNegativeMerchants[0].Zone}\n";
            body += $"Card: {nonNegativeMerchants[0].Card.Name}\n";
            body += $"Rapport: {nonNegativeMerchants[0].Rapport.Name}\n";
        }

        await CreateNotification($"Wandering Merchant \"{merchantGroup.MerchantName}\" found", new { Body = body, Renotify = true, Tag = $"found_{merchantGroup.MerchantName}", Icon = "images/notifications/ExclamationMark.png" });
    }

    /// <summary>
    /// Check if merchant spawns are notified for any of the passed in merchants.
    /// </summary>
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

    private async ValueTask CreateNotification(string title, object parameters)
    {
        var notification = await _jsRuntime.InvokeAsync<IJSObjectReference>("Create", title, parameters);
        _notifications.Add(notification);
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

    public async Task RunTestNotification()
    {
        if (_clientSettings.NotifyBrowserSoundEnabled)
        {
            await PlaySound();
        }
        await CreateNotification($"Wandering Merchant Test found", new { Body = "This is only a test", Renotify = true, Tag = $"item", Icon = "images/notifications/ExclamationMark.png" });
    }

    public ValueTask PlaySound()
    {
        try
        {
            return _jsRuntime.InvokeVoidAsync("PlayNotificationSound", _clientSettings.SoundVolume);
        }
        catch (Exception)
        {
            //ignore
            //if the sound doesn't play... whatever. No need to let the whole session crash.
        }
        return ValueTask.CompletedTask;
    }
}
