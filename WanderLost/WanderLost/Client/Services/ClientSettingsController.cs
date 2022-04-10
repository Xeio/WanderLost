using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Services
{
    public class ClientSettingsController
    {
        public string Region { get; private set; } = string.Empty;
        public string Server { get; private set; } = string.Empty;
        public bool NotificationsEnabled { get; private set; }
        public bool NotifyMerchantAppearance { get; private set; }
        public bool NotifyLegendaryRapport { get; private set; }
        public bool NotifyBrowserSoundEnabled { get; private set; }
        public Dictionary<string, MerchantNotificationSetting> Notifications { get; private set; } = new();

        private bool _initialized = false;

        private readonly ILocalStorageService _localStorageService;

        public ClientSettingsController(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        public async Task Init()
        {
            if (!_initialized)
            {
                Region = await _localStorageService.GetItemAsync<string?>(nameof(Region)) ?? string.Empty;
                Server = await _localStorageService.GetItemAsync<string?>(nameof(Server)) ?? string.Empty;
                NotificationsEnabled = await _localStorageService.GetItemAsync<bool?>(nameof(NotificationsEnabled)) ?? false;
                NotifyMerchantAppearance = await _localStorageService.GetItemAsync<bool?>(nameof(NotifyMerchantAppearance)) ?? false;
                NotifyLegendaryRapport = await _localStorageService.GetItemAsync<bool?>(nameof(NotifyLegendaryRapport)) ?? false;
                NotifyBrowserSoundEnabled = await _localStorageService.GetItemAsync<bool?>(nameof(NotifyBrowserSoundEnabled)) ?? false;
                Notifications = await _localStorageService.GetItemAsync<Dictionary<string, MerchantNotificationSetting>?>(nameof(Notifications)) ?? new();
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

        public async Task SetNotifyMerchantAppearance(bool notifyMerchantAppearance)
        {
            NotifyMerchantAppearance = notifyMerchantAppearance;
            await _localStorageService.SetItemAsync(nameof(NotifyMerchantAppearance), notifyMerchantAppearance);
        }

        public async Task SetNotifyLegendaryRapport(bool notifyLegendaryRapport)
        {
            NotifyLegendaryRapport = notifyLegendaryRapport;
            await _localStorageService.SetItemAsync(nameof(NotifyLegendaryRapport), notifyLegendaryRapport);
        }

        public async Task SetNotifyBrowserSoundEnabled(bool soundEnabled)
        {
            NotifyBrowserSoundEnabled = soundEnabled;
            await _localStorageService.SetItemAsync(nameof(NotifyBrowserSoundEnabled), soundEnabled);
        }

        public async Task SaveNotificationSettings()
        {
            await _localStorageService.SetItemAsync(nameof(Notifications), Notifications);
        }
    }
}
