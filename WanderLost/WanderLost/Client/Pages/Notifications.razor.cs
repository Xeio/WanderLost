using Microsoft.AspNetCore.Components;
using WanderLost.Client.Services;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Pages
{
    public partial class Notifications
    {
        [Inject] public ClientSettingsController ClientSettings { get; init; } = default!;
        [Inject] public ClientStaticDataController StaticData { get; init; } = default!;
        [Inject] public NavigationManager NavigationManager { get; init; } = default!;
        [Inject] public ClientNotificationService ClientNotifications { get; init; } = default!;

        protected override async Task OnInitializedAsync()
        {
            await StaticData.Init();
            await ClientSettings.Init();

            await base.OnInitializedAsync();
        }

        protected async Task ToggleNotifyBrowserSoundEnabled()
        {
            await ClientSettings.SetNotifyBrowserSoundEnabled(!ClientSettings.NotifyBrowserSoundEnabled);
        }

        protected async Task OnTestMerchantSpawnClicked()
        {
            var dummyData = new MerchantData
            {
                Name = "Lailai",
                Zones = new List<string> { "Punika" },
                Cards = new List<Item> { new Item { Name = "Shut up!!", Rarity = Rarity.Rare } },
            };
            var dummyMerchantGroup = new ActiveMerchantGroup
            {
                MerchantData = dummyData,
                MerchantName = "Lailai",
                ActiveMerchants = new List<ActiveMerchant> { new ActiveMerchant { Name = "Lailai", Card = dummyData.Cards[0], Zone = dummyData.Zones[0] } },
            };
            await ClientNotifications.ForceMerchantSpawnNotification(dummyMerchantGroup);
        }

        protected async Task OnTestMerchantFoundClicked()
        {
            var dummyData = new MerchantData
            {
                Name = "Lailai",
                Zones = new List<string> { "Punika" },
                Cards = new List<Item> { new Item { Name = "Shut up!!", Rarity = Rarity.Rare } },
                Rapports = new List<Item> { new Item { Name = "Flower", Rarity = Rarity.Rare } },
            };
            var dummyMerchantGroup = new ActiveMerchantGroup
            {
                MerchantData = dummyData,
                MerchantName = "Lailai",
                ActiveMerchants = new List<ActiveMerchant> { new ActiveMerchant { Name = "Lailai", Card = dummyData.Cards[0], Zone = dummyData.Zones[0], Rapport = dummyData.Rapports[0] } },
            };
            await ClientNotifications.ForceMerchantFoundNotification(dummyMerchantGroup);
        }

        protected async Task OnNotificationToggle(NotificationSettingType category, string merchant, object value)
        {
            if (!ClientSettings.Notifications.TryGetValue(merchant, out var notificationSetting))
            {
                notificationSetting = ClientSettings.Notifications[merchant] = new();
            }

            if (category == NotificationSettingType.Spawn)
            {
                notificationSetting.NotifySpawn = !notificationSetting.NotifySpawn;
            }
            else if (category == NotificationSettingType.Card && value is Item card)
            {
                if (notificationSetting.Cards.Contains(card.Name))
                {
                    notificationSetting.Cards.Remove(card.Name);
                }
                else
                {
                    notificationSetting.Cards.Add(card.Name);
                }
            }
            else if (category == NotificationSettingType.Rapport && value is Item rapport)
            {
                if (notificationSetting.Rapports.Contains(rapport.Name))
                {
                    notificationSetting.Rapports.Remove(rapport.Name);
                }
                else
                {
                    notificationSetting.Rapports.Add(rapport.Name);
                }
            }

            await ClientSettings.SaveNotificationSettings();
        }

        protected bool IsSpawnNotified(string name)
        {
            return ClientSettings.Notifications.TryGetValue(name, out var setting) && setting.NotifySpawn;
        }

        protected bool IsMerchantValueNotified(string name, NotificationSettingType category, object value)
        {
            if (!ClientSettings.Notifications.TryGetValue(name, out var setting))
            {
                return false;
            }

            if (category == NotificationSettingType.Spawn)
            {
                return setting.NotifySpawn;
            }

            if (category == NotificationSettingType.Card && value is Item card)
            {
                return setting.Cards.Contains(card.Name);
            }

            if (category == NotificationSettingType.Rapport && value is Item rapport)
            {
                return setting.Rapports.Contains(rapport.Name);
            }

            return false;
        }
    }
}
