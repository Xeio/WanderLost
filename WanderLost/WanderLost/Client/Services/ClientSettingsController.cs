using Blazored.LocalStorage;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Services;

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
    public PushSubscription? SavedPushSubscription { get; private set; }
    public bool BrowserNotifications { get; private set; }
    public bool RareSoundsOnly { get; private set; }
    public bool CollapseCards { get; private set; }

    private bool _initialized = false;

    private readonly ILocalStorageService _localStorageService;
    private readonly ClientStaticDataController _staticData;

    private readonly Dictionary<string, string> _serverMerges = new()
    {
        { "Shadespire", "Rethramis" },
        { "Petrania", "Tortoyk" },
        { "Tragon", "Moonkeep" },
        { "Stonehearth", "Punika" },
        { "Kurzan", "Agaton" },
        { "Prideholme", "Vern" },
        { "Yorn", "Gienah" },
        { "Feiton", "Arcturus" },
        { "Sirius", "Armen" },
        { "Sceptrum", "Armen" },
        { "Thaemine", "Lazenith" },
        { "Procyon", "Lazenith" },
        { "Nineveh", "Evergrace" },
        { "Beatrice", "Evergrace" },
        { "Brelshaza", "Ezrebet" },
        { "Inanna", "Ezrebet" },
    };

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
            SavedPushSubscription = await _localStorageService.GetItemAsync<PushSubscription?>(nameof(SavedPushSubscription));
            RareSoundsOnly = await _localStorageService.GetItemAsync<bool?>(nameof(RareSoundsOnly)) ?? false;
            BrowserNotifications = await _localStorageService.GetItemAsync<bool?>(nameof(BrowserNotifications)) ?? false;
            CollapseCards = await _localStorageService.GetItemAsync<bool?>(nameof(CollapseCards)) ?? false;

            if(_serverMerges.TryGetValue(Server, out var newServer))
            {
                //Compatability for old servers after merge, will auto-select new server
                await SetServer(newServer);
            }

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

    public async Task SetRareSoundsOnly(bool rareSoundsOnly)
    {
        RareSoundsOnly = rareSoundsOnly;
        await _localStorageService.SetItemAsync(nameof(RareSoundsOnly), rareSoundsOnly);
    }

    public async Task SetSavedPushSubscription(PushSubscription? subscription)
    {
        if (subscription == null)
        {
            await _localStorageService.RemoveItemAsync(nameof(SavedPushSubscription));
        }
        else
        {
            subscription.SendTestNotification = false; //Don't allow saving a "test" setting, in case it's ever set
            SavedPushSubscription = subscription;
            await _localStorageService.SetItemAsync(nameof(SavedPushSubscription), subscription);
        }
    }

    public async Task SetBrowserNotifications(bool browserNotifications)
    {
        BrowserNotifications = browserNotifications;
        await _localStorageService.SetItemAsync(nameof(BrowserNotifications), browserNotifications);
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

    public async Task SetCollapseCards(bool collapseCards)
    {
        CollapseCards = collapseCards;
        await _localStorageService.SetItemAsync(nameof(CollapseCards), collapseCards);
    }
}
