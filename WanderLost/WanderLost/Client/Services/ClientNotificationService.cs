using Microsoft.JSInterop;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Services
{
    public sealed class ClientNotificationService : IAsyncDisposable
    {
        private readonly ClientSettingsController _clientSettings;
        private readonly IJSRuntime _jsRuntime;
        private readonly List<IJSObjectReference> _notifications = new();

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
            return _jsRuntime.InvokeAsync<bool>("SupportsNotifications");
        }

        /// <summary>
        /// Request permission to send notifications from user.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<bool> RequestPermission()
        {
            var permissionResult = await _jsRuntime.InvokeAsync<string>("RequestPermission");
            return permissionResult == "granted";
        }

        private bool IsAllowedForMerchantFoundNotifications(ActiveMerchantGroup merchantGroup)
        {
            if (merchantGroup.ActiveMerchants.Count == 0) return false;

            if (!_clientSettings.Notifications.TryGetValue(merchantGroup.MerchantName, out var notificationSetting))
            {
                return false;
            }

            foreach (var card in merchantGroup.ActiveMerchants.Select(m => m.Card))
            {
                if (notificationSetting.Cards.Contains(card.Name)) return true;
            }

            foreach (var rapport in merchantGroup.ActiveMerchants.Select(m => m.Rapport))
            {
                if (notificationSetting.Rapports.Contains(rapport.Name)) return true;
            }

            return false;
        }

        /// <summary>
        /// Request a "merchant found" Browser-Notification for the given merchantGroup, rules from usersettings are applied; the request can be denied.
        /// </summary>
        /// <param name="merchantGroup"></param>
        /// <returns></returns>
        public ValueTask RequestMerchantFoundNotification(ActiveMerchantGroup merchantGroup)
        {
            if (!_clientSettings.NotificationsEnabled) return ValueTask.CompletedTask;
            if (!IsAllowedForMerchantFoundNotifications(merchantGroup)) return ValueTask.CompletedTask;

            return ForceMerchantFoundNotification(merchantGroup);
        }
        /// <summary>
        /// Force a "merchant found" Browser-Notification for the given merchantGroup, rules from usersettings are NOT applied.
        /// </summary>
        /// <param name="merchantGroup"></param>
        /// <returns></returns>
        public ValueTask ForceMerchantFoundNotification(ActiveMerchantGroup merchantGroup)
        {
            RequestBrowserNotificationSound();

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

            return CreateNotification($"Wandering Merchant \"{merchantGroup.MerchantName}\" found", new { Body = body, Renotify = true, Tag = $"found_{merchantGroup.MerchantName}", Icon = "images/notifications/ExclamationMark.png" });
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
        public ValueTask ForceMerchantSpawnNotification(ActiveMerchantGroup merchantGroup)
        {
            RequestBrowserNotificationSound();

            string body = $"Wandering Merchant \"{merchantGroup.MerchantName}\" is waiting for you somewhere.";
            return CreateNotification($"Wandering Merchant \"{merchantGroup.MerchantName}\" appeared", new { Body = body, Renotify = true, Tag = "spawn_merchant", Icon = "images/notifications/QuestionMark.png" });
        }

        private async ValueTask CreateNotification(string title, object parameters)
        {
            var notification = await _jsRuntime.InvokeAsync<IJSObjectReference>("Create", title, parameters);
            _notifications.Add(notification);
        }

        private async void RequestBrowserNotificationSound()
        {
            if (!_clientSettings.NotifyBrowserSoundEnabled) return;

            try
            {
                await _jsRuntime.InvokeAsync<string>("PlayNotificationSound"); //call Interop.js function to play a sound
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
    }
}
