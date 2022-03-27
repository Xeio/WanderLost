using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
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

            if(ClientSettings.NotifyingMerchants.Count == 0)
            {
                await ClientSettings.SetNotifyingMerchants(BuildNotifyingMerchantsPreset());
            }
        }

        protected async Task ToggleNotifyAppearance()
        {
            await ClientSettings.SetNotifyMerchantAppearance(!ClientSettings.NotifyMerchantAppearance);
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
            bool changed = false;
            if (category == nameof(StaticData.Merchants))
            {
                if (ClientSettings.NotifyingMerchants.FirstOrDefault(x => x.Name == value as string) is MerchantData existing)
                {
                    if (!setActive)
                    {
                        ClientSettings.NotifyingMerchants.Remove(existing);
                        changed = true;
                    }
                }
                else
                {
                    if (setActive && StaticData.Merchants.FirstOrDefault(x => x.Key == value as string).Value is MerchantData mData)
                    {
                        ClientSettings.NotifyingMerchants?.Add(mData);
                        changed = true;
                    }
                }
            }
            else if (category == nameof(MerchantData.Zones))
            {
                if (ClientSettings.NotifyingMerchants.FirstOrDefault(x => x.Name == merchant) is MerchantData existing)
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
                if (ClientSettings.NotifyingMerchants.FirstOrDefault(x => x.Name == merchant) is MerchantData existing)
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
                await ClientSettings.SetNotifyingMerchants(ClientSettings.NotifyingMerchants);
            }
            StateHasChanged();
        }

        private List<MerchantData> BuildNotifyingMerchantsPreset()
        {
            return StaticData.Merchants.Select(x => x.Value).ToList();
        }

        protected bool IsMerchantNotified(string name)
        {
            return ClientSettings.NotifyingMerchants.Any(x => x.Name == name);
        }

        protected bool IsMerchantValueNotified(string name, string category, object value)
        {
            if (category == nameof(MerchantData.Zones))
            {
                return ClientSettings.NotifyingMerchants.FirstOrDefault(x => x.Name == name)?.Zones.Any(x => x == value as string) ?? false;
            }
            else if (category == nameof(MerchantData.Cards))
            {
                return ClientSettings.NotifyingMerchants.FirstOrDefault(x => x.Name == name)?.Cards.Any(x => x.Name == (value as Item)?.Name) ?? false;
            }
            else
            {
                throw new ArgumentException($"unknown {nameof(category)}.");
            }
        }

        protected bool HasNotifiedMerchantValues(string name)
        {
            return ClientSettings.NotifyingMerchants.Any(x => x.Name == name);
        }
    }
}
