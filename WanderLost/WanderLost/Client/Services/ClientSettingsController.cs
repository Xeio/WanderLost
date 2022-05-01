using Blazored.LocalStorage;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Services
{
    public class ClientSettingsController
    {
        public string Region { get; private set; } = string.Empty;
        public string Server { get; private set; } = string.Empty;
        public bool NotificationsEnabled { get; private set; }
        public bool NotifyBrowserSoundEnabled { get; private set; }
        public Dictionary<string, MerchantNotificationSetting> Notifications { get; private set; } = new();
        public Dictionary<Rarity, int> CardVoteThresholdForNotification { get; set; } = new();
        public Dictionary<Rarity, int> RapportVoteThresholdForNotification { get; set; } = new();
        public int LastDisplayedMessageId { get; private set; }
        public float SoundVolume { get; private set; }

        private bool _initialized = false;

        private readonly ILocalStorageService _localStorageService;
        private readonly ClientStaticDataController _staticData;

        public ClientSettingsController(ILocalStorageService localStorageService, ClientStaticDataController staticData)
        {
            _localStorageService = localStorageService;
            _staticData = staticData;
        }

        public async Task Init()
        {
            if (!_initialized)
            {
                await _staticData.Init();

                Region = await _localStorageService.GetItemAsync<string?>(nameof(Region)) ?? string.Empty;
                Server = await _localStorageService.GetItemAsync<string?>(nameof(Server)) ?? string.Empty;
                NotificationsEnabled = await _localStorageService.GetItemAsync<bool?>(nameof(NotificationsEnabled)) ?? false;
                NotifyBrowserSoundEnabled = await _localStorageService.GetItemAsync<bool?>(nameof(NotifyBrowserSoundEnabled)) ?? false;
                Notifications = await _localStorageService.GetItemAsync<Dictionary<string, MerchantNotificationSetting>?>(nameof(Notifications)) ?? new();
                CardVoteThresholdForNotification = await _localStorageService.GetItemAsync<Dictionary<Rarity, int>?>(nameof(CardVoteThresholdForNotification)) ?? new();
                RapportVoteThresholdForNotification = await _localStorageService.GetItemAsync<Dictionary<Rarity, int>?>(nameof(RapportVoteThresholdForNotification)) ?? new();
                LastDisplayedMessageId = await _localStorageService.GetItemAsync<int?>(nameof(LastDisplayedMessageId)) ?? 0;
                SoundVolume = await _localStorageService.GetItemAsync<float?>(nameof(SoundVolume)) ?? 1f;

                //Compatability to convert/remove old settings to items
                if (await _localStorageService.GetItemAsync<bool?>("NotifyLegendaryRapport") ?? false)
                {
                    foreach(var merchant in _staticData.Merchants.Values)
                    {
                        if(!Notifications.TryGetValue(merchant.Name, out var notificationSetting))
                        {
                            Notifications[merchant.Name] = notificationSetting = new();
                        }
                        foreach (var rapport in merchant.Rapports.Where(r => r.Rarity == Rarity.Legendary))
                        {
                            notificationSetting.Rapports.Add(rapport.Name);
                        }
                    }
                    await SaveNotificationSettings();
                }
                await _localStorageService.RemoveItemAsync("NotifyLegendaryRapport");
                await _localStorageService.RemoveItemAsync("NotifyMerchantAppearance");


                _initialized = true;
            }
        }

        public async Task SetRegion(string region)
        {
            Region = region;
            await _localStorageService.SetItemAsync(nameof(Region), region);
        }

        public async Task SetServer(string server)
        {
            Server = server;
            await _localStorageService.SetItemAsync(nameof(Server), server);
        }

        public async Task SetNotificationsEnabled(bool notificationsEnabled)
        {
            NotificationsEnabled = notificationsEnabled;
            await _localStorageService.SetItemAsync(nameof(NotificationsEnabled), notificationsEnabled);
        }

        public async Task SetNotifyBrowserSoundEnabled(bool soundEnabled)
        {
            NotifyBrowserSoundEnabled = soundEnabled;
            await _localStorageService.SetItemAsync(nameof(NotifyBrowserSoundEnabled), soundEnabled);
        }

        public async Task SetLastDisplayedMessageId(int messageId)
        {
            LastDisplayedMessageId = messageId;
            await _localStorageService.SetItemAsync(nameof(LastDisplayedMessageId), messageId);
        }

        public async Task SetSoundVolume(float volume)
        {
            SoundVolume = volume;
            await _localStorageService.SetItemAsync(nameof(SoundVolume), volume);
        }

        public async Task SaveNotificationSettings()
        {
            await _localStorageService.SetItemAsync(nameof(Notifications), Notifications);
        }

        public async Task SaveCardVoteThresholdForNotification()
        {
            await _localStorageService.SetItemAsync(nameof(CardVoteThresholdForNotification), CardVoteThresholdForNotification);
        }

        public async Task SaveRapportVoteThresholdForNotification()
        {
            await _localStorageService.SetItemAsync(nameof(RapportVoteThresholdForNotification), RapportVoteThresholdForNotification);
        }
    }
}
