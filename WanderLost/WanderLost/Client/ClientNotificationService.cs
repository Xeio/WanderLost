using Append.Blazor.Notifications;
using WanderLost.Shared.Data;

namespace WanderLost.Client
{
    public class ClientNotificationService
    {
        public bool NotificationsAvailable { get; private set; } = false;

        private INotificationService _notifications;
        private ClientData _userSettings = new ClientData();
        public ClientNotificationService(INotificationService notif)
        {
            _notifications = notif;
        }

        public async Task Init(ClientData? userSettings)
        {
            if (userSettings != null)
            {
                _userSettings = userSettings;
            }

            if (NotificationsAvailable) return;

            if (await _notifications.IsSupportedByBrowserAsync())
            {
                if (_notifications.PermissionStatus == PermissionType.Granted)
                {
                    NotificationsAvailable = true;
                }
                else
                {
                    if (await _notifications.RequestPermissionAsync() is PermissionType answer && answer == PermissionType.Granted)
                    {
                        NotificationsAvailable = true;
                    }
                }
            }
        }

        public ValueTask RequestMerchantFoundNotification(ActiveMerchantGroup merchantGroup)
        {
            if (merchantGroup == null) return ValueTask.CompletedTask;
            if (merchantGroup.ActiveMerchants.Count == 0) return ValueTask.CompletedTask;

            //Check _userSettings if merchants in merchantGroup are allowed for notifications.
            if (_userSettings.NotifyingMerchants != null)
            {
                if (!_userSettings.NotifyingMerchants.Any(allowedMerch => allowedMerch.Name == merchantGroup.MerchantName)) return ValueTask.CompletedTask;
                if (!_userSettings.NotifyingMerchants.Where(allowedMerch => allowedMerch.Name == merchantGroup.MerchantName)
                                                        .Any(x => x.Zones.Any(allowedZone => merchantGroup.ActiveMerchants.Any(actMerch => actMerch.Zone == allowedZone)))) return ValueTask.CompletedTask;
                if (!_userSettings.NotifyingMerchants.Where(allowedMerch => allowedMerch.Name == merchantGroup.MerchantName)
                                                        .Any(x => x.Cards.Any(allowedCard => merchantGroup.ActiveMerchants.Any(actMerch => actMerch.Card.Name == allowedCard.Name)))) return ValueTask.CompletedTask;
            }

            string body = "";
            if (merchantGroup.ActiveMerchants.Count > 1)
            {
                body += "Conflicting merchant data, click for more information.";
            }
            else
            {
                body += $"Location: {merchantGroup.ActiveMerchants[0].Zone}\n";
                body += $"Card: {merchantGroup.ActiveMerchants[0].Card.Name}\n";
                body += $"Rapport: {merchantGroup.ActiveMerchants[0].RapportRarity?.ToString() ?? "_unknown"}\n";
            }

            return _notifications.CreateAsync($"Wandering Merchant \"{merchantGroup.MerchantName}\" found", new NotificationOptions { Body = body, Renotify = true, Tag = $"found_{merchantGroup.MerchantName}", Icon = "images/notifications/ExclamationMark.png" });
        }

        public ValueTask RequestMerchantSpawnNotification(ActiveMerchantGroup merchantGroup)
        {
            if (merchantGroup == null) return ValueTask.CompletedTask;
            if (!_userSettings.NotifyMerchantAppearance) return ValueTask.CompletedTask;

            string body = $"Wandering Merchant \"{merchantGroup.MerchantName}\" is waiting for you somewhere.";
            return _notifications.CreateAsync($"Wandering Merchant \"{merchantGroup.MerchantName}\" appeared", new NotificationOptions { Body = body, Renotify = true, Tag = "spawn_merchant", Icon = "images/notifications/QuestionMark.png" });
        }
    }
}
