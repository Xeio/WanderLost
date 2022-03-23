using Append.Blazor.Notifications;
using WanderLost.Shared.Data;

namespace WanderLost.Client
{
    public class ClientNotificationService
    {
        public bool NotificationsAvailable { get; private set; } = false;

        private INotificationService _notifications;
        public ClientNotificationService(INotificationService notif)
        {
            _notifications = notif;
        }

        public async Task Init()
        {
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

        public ValueTask CreateMerchantFoundNotification(ActiveMerchantGroup merchantGroup)
        {
            if (merchantGroup == null) return new ValueTask();

            string body = $"Wandering Merchant \"{merchantGroup.MerchantName}\" was found at {merchantGroup.ActiveMerchants.FirstOrDefault()?.Zone ?? "_unkown"}.";
            return _notifications.CreateAsync($"Wandering Merchant \"{merchantGroup.MerchantName}\" found", new NotificationOptions { Body = body, Renotify = true, Icon = "images/notifications/ExclamationMark.png" });
        }

        public ValueTask CreateMerchantSpawnNotification(ActiveMerchantGroup merchantGroup)
        {
            if (merchantGroup == null) return new ValueTask();

            string body = $"Wandering Merchant \"{merchantGroup.MerchantName}\" is waiting for you somewhere.";
            return _notifications.CreateAsync($"Wandering Merchant \"{merchantGroup.MerchantName}\" appeared", new NotificationOptions { Body = body, Renotify = true, Icon = "images/notifications/QuestionMark.png" });
        }
    }
}
