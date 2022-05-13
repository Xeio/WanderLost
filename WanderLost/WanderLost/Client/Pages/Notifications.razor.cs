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

            _cardVoteThresholdWrapper = ClientSettings.CardVoteThresholdForNotification.GetValueOrDefault(Rarity.Legendary);
            _rapportVoteThresholdWrapper = ClientSettings.RapportVoteThresholdForNotification.GetValueOrDefault(Rarity.Legendary);
            await base.OnInitializedAsync();
        }

        protected async Task ToggleNotifyBrowserSoundEnabled()
        {
            await ClientSettings.SetNotifyBrowserSoundEnabled(!ClientSettings.NotifyBrowserSoundEnabled);
        }

        public async Task ToggleBrowserNotifications()
        {
            if (ClientSettings.BrowserNotifications)
            {
                await ClientSettings.SetBrowserNotifications(false);
            }
            else
            {
                if (await ClientNotifications.IsSupportedByBrowser() && await ClientNotifications.RequestPermission())
                {
                    await ClientSettings.SetBrowserNotifications(true);
                }
            }
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
            await ClientNotifications.CheckBrowserNotification(dummyMerchantGroup);
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

        private async void SetCardVoteThreshold(Rarity rarity, int newThreshold)
        {
            ClientSettings.CardVoteThresholdForNotification[rarity] = newThreshold;
            await ClientSettings.SaveCardVoteThresholdForNotification();
        }

        private async void SetRapportVoteThreshold(Rarity rarity, int newThreshold)
        {
            ClientSettings.RapportVoteThresholdForNotification[rarity] = newThreshold;
            await ClientSettings.SaveRapportVoteThresholdForNotification();
        }

        private int _cardVoteThresholdWrapper;

        public int CardVoteThresholdWrapper
        {
            get { return _cardVoteThresholdWrapper; }
            set
            {
                if (value < 0) value = 0;
                _cardVoteThresholdWrapper = value;
                SetCardVoteThreshold(Rarity.Legendary, value);
            }
        }

        private int _rapportVoteThresholdWrapper;

        public int RapportVoteThresholdWrapper
        {
            get { return _rapportVoteThresholdWrapper; }
            set
            {
                if (value < 0) value = 0;
                _rapportVoteThresholdWrapper = value;
                SetRapportVoteThreshold(Rarity.Legendary, value);
            }
        }

        public async Task ChangeVolumeAndPlayTestSound(ChangeEventArgs args)
        {
            if (args.Value is string s && int.TryParse(s, out var i))
            {
                float volume = i / 100f;
                await ClientSettings.SetSoundVolume(volume);
                await ClientNotifications.CheckBrowserNotificationSound();
            }
        }
    }
}
