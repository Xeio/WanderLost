using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Pages
{
    public partial class Notifications
    {
        [Inject] public ILocalStorageService LocalStorage { get; set; } = default!; //default! to suppress NULL warning
        [Inject] public ClientStaticDataController StaticData { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public ClientNotificationService ClientNotifications { get; set; } = default!; //default! to suppress NULL warning

        private ClientData _clientData = new();
        bool _notificationsDisabledByBrowser = false;
        protected override async Task OnInitializedAsync()
        {
            await StaticData.Init();
            await Init();
            await base.OnInitializedAsync();
            StateHasChanged();
        }

        private async Task Init()
        {
            await TryInitClientData();
            if (_clientData.NotifyingMerchants == null)
            {
                _clientData.NotifyingMerchants = BuildNotifyingMerchantsPreset();
                await SaveClientData();
                await TryInitClientData();
            }
            HandleClientNotificationInit();
        }

        private void HandleClientNotificationInit()
        {
            if (_clientData?.NotificationsEnabled == true)
            {
                _notificationsDisabledByBrowser = false;
            }
            else
            {
                _notificationsDisabledByBrowser = true;
            }
            StateHasChanged();
        }

        private async Task TryInitClientData()
        {
            var cd = await LocalStorage.GetItemAsync<ClientData?>(nameof(ClientData));
            //if ClientData is not set or no server is specified yet, go back to mainpage, no point in being here.
            if (cd == null || string.IsNullOrWhiteSpace(cd.Server))
                NavigationManager.NavigateTo(NavigationManager.BaseUri);
            else
                _clientData = cd;
        }

        protected async Task OnNotificationStateChanged(bool value)
        {
            await Init();
            StateHasChanged();
        }

        private ValueTask SaveClientData()
        {
            return LocalStorage.SetItemAsync(nameof(ClientData), _clientData);
        }

        protected async Task OnToggleNotifyAppearanceClicked()
        {
            _clientData.NotifyMerchantAppearance = !_clientData.NotifyMerchantAppearance;
            await SaveClientData();
            StateHasChanged();
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

        protected async Task OnNotificationStateChanged(bool setActive, string category, string merchant, object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (_clientData == null) return;
            if (StaticData.Merchants == null) return;
            if (_clientData.NotifyingMerchants == null) return;

            bool changed = false;
            if (category == nameof(StaticData.Merchants))
            {
                if (_clientData?.NotifyingMerchants?.FirstOrDefault(x => x.Name == value as string) is MerchantData existing)
                {
                    if (!setActive)
                    {
                        _clientData.NotifyingMerchants.Remove(existing);
                        changed = true;
                    }
                }
                else
                {
                    if (setActive && StaticData.Merchants.FirstOrDefault(x => x.Key == value as string).Value is MerchantData mData)
                    {
                        _clientData?.NotifyingMerchants?.Add(mData);
                        changed = true;
                    }
                }
            }
            else if (category == nameof(MerchantData.Zones))
            {
                if (_clientData?.NotifyingMerchants?.FirstOrDefault(x => x.Name == merchant) is MerchantData existing)
                {
                    if (existing.Zones.FirstOrDefault(x => x == value as string) is string zone)
                    {
                        if (!setActive)
                        {
                            existing.Zones.Remove(zone);
                            changed = true;
                        }
                    }
                    else if (setActive)
                    {
                        if (value as string is string valueStr)
                        {
                            existing.Zones.Add(valueStr);
                            changed = true;
                        }
                    }
                }
            }
            else if (category == nameof(MerchantData.Cards))
            {
                if (_clientData?.NotifyingMerchants?.FirstOrDefault(x => x.Name == merchant) is MerchantData existing)
                {
                    if (existing.Cards.FirstOrDefault(x => x.Name == (value as Item)?.Name) is Item card)
                    {
                        if (!setActive)
                        {
                            existing.Cards.Remove(card);
                            changed = true;
                        }
                    }
                    else if (setActive)
                    {
                        if (value as Item is Item valueCard)
                        {
                            existing.Cards.Add(valueCard);
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                //save to cookies
                await SaveClientData();
            }
            StateHasChanged();
        }

        private List<MerchantData> BuildNotifyingMerchantsPreset()
        {
            return StaticData.Merchants.Select(x => x.Value).ToList();
        }

        protected bool IsMerchantNotified(string name)
        {
            if (_clientData == null) return true;
            if (_clientData.NotifyingMerchants == null) return true;

            return _clientData.NotifyingMerchants.Any(x => x.Name == name);
        }

        protected bool IsMerchantValueNotified(string name, string category, object value)
        {
            if (_clientData == null) return true;
            if (_clientData.NotifyingMerchants == null) return true;

            if (category == nameof(MerchantData.Zones))
            {
                return _clientData.NotifyingMerchants.FirstOrDefault(x => x.Name == name)?.Zones.Any(x => x == value as string) ?? false;
            }
            else if (category == nameof(MerchantData.Cards))
            {
                return _clientData.NotifyingMerchants.FirstOrDefault(x => x.Name == name)?.Cards.Any(x => x.Name == (value as Item)?.Name) ?? false;
            }
            else
            {
                throw new ArgumentException($"unknown {nameof(category)}.");
            }
        }

        protected bool HasNotifiedMerchantValues(string name)
        {
            if (_clientData == null) return false;
            if (_clientData.NotifyingMerchants == null) return false;

            return _clientData.NotifyingMerchants.Any(x => x.Name == name);
        }

        public bool NotifyAppearanceWrapper
        {
            get { return _clientData.NotifyMerchantAppearance; }
            set 
            {
                _ = OnToggleNotifyAppearanceClicked();
            }
        }

    }
}
