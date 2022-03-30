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

        protected async Task ToggleNotifyAppearance()
        {
            await ClientSettings.SetNotifyMerchantAppearance(!ClientSettings.NotifyMerchantAppearance);
        }

        protected async Task ToggleNotifyLegendaryRapport()
        {
            await ClientSettings.SetNotifyLegendaryRapport(!ClientSettings.NotifyLegendaryRapport);
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
            };
            var dummyMerchantGroup = new ActiveMerchantGroup
            {
                MerchantData = dummyData,
                MerchantName = "Lailai",
                ActiveMerchants = new List<ActiveMerchant> { new ActiveMerchant { Name = "Lailai", Card = dummyData.Cards[0], Zone = dummyData.Zones[0], RapportRarity = Rarity.Rare } },
            };
            await ClientNotifications.ForceMerchantFoundNotification(dummyMerchantGroup);
        }

        protected async Task OnNotificationToggle(bool setActive, NotificationSettingType category, string merchant, object value)
        {
            if (!ClientSettings.Notifications.TryGetValue(merchant, out var notificationSetting))
            {
                notificationSetting = ClientSettings.Notifications[merchant] = new();
            }

            if (category == NotificationSettingType.Merchant)
            {
                notificationSetting.Enabled = !notificationSetting.Enabled;
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
                    //Also force-enable the merchant, if a user is trying to notify based on an item that merchant carries
                    if (!notificationSetting.Enabled) notificationSetting.Enabled = true;
                }
            }

            await ClientSettings.SaveNotificationSettings();
        }

        protected bool IsMerchantNotified(string name)
        {
            if(ClientSettings.Notifications.TryGetValue(name, out var setting))
            {
                return setting.Enabled;
            }
            return false;
        }

        protected bool IsMerchantValueNotified(string name, NotificationSettingType category, object value)
        {
            if (!ClientSettings.Notifications.TryGetValue(name, out var setting))
            {
                return false;
            }

            if (category == NotificationSettingType.Card && value is Item card)
            {
                return setting.Cards.Contains(card.Name);
            }

            return false;
        }
    }
}
