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

        private ClientData? ClientData;
        protected override async Task OnInitializedAsync()
        {
            await StaticData.Init();
            ClientData = await LocalStorage.GetItemAsync<ClientData?>(nameof(ClientData));

            if (ClientData != null && ClientData.NotifyingMerchants == null)
            {
                await LocalStorage.RemoveItemAsync(nameof(ClientData));
                ClientData.NotifyingMerchants = buildNotifyingMerchantsPreset();
                var json = JsonSerializer.Serialize(ClientData.NotifyingMerchants);
                var desjon = JsonSerializer.Deserialize<List<MerchantData>?>(json);
                await LocalStorage.SetItemAsync(nameof(ClientData), ClientData);
            }
            await base.OnInitializedAsync();
        }

        protected async Task OnNotificationStateChanged(bool setActive, string category, string merchant, object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (ClientData == null) return;
            if (StaticData.Merchants == null) return;
            if (ClientData.NotifyingMerchants == null)
            {
                ClientData.NotifyingMerchants = buildNotifyingMerchantsPreset();
            }

            bool changed = false;
            if (category == nameof(StaticData.Merchants))
            {
                if (ClientData?.NotifyingMerchants?.FirstOrDefault(x => x.Name == value as string) is MerchantData existing)
                {
                    if (!setActive)
                    {
                        ClientData.NotifyingMerchants.Remove(existing);
                        changed = true;
                    }
                }
                else
                {
                    if (setActive && StaticData.Merchants.FirstOrDefault(x => x.Key == value as string).Value is MerchantData mData)
                    {
                        ClientData?.NotifyingMerchants?.Add(mData);
                        changed = true;
                    }
                }
            }
            else if (category == nameof(MerchantData.Zones))
            {
                if (ClientData?.NotifyingMerchants?.FirstOrDefault(x => x.Name == merchant) is MerchantData existing)
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
                if (ClientData?.NotifyingMerchants?.FirstOrDefault(x => x.Name == merchant) is MerchantData existing)
                {
                    if (existing.Cards.FirstOrDefault(x => x.Name == (value as Item)?.Name) is Item card)
                    {
                        if (!setActive)
                        {
                            existing.Cards.Remove(card);
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
                await LocalStorage.SetItemAsync(nameof(ClientData), ClientData);
            }
        }

        private List<MerchantData> buildNotifyingMerchantsPreset()
        {
            return StaticData.Merchants.Select(x => x.Value).ToList();
        }

        protected bool IsMerchantNotified(string name)
        {
            if (ClientData == null) return true;
            if (ClientData.NotifyingMerchants == null) return true;

            return ClientData.NotifyingMerchants.Any(x => x.Name == name);
        }

        protected bool IsMerchantValueNotified(string name, string category, object value)
        {
            if (ClientData == null) return true;
            if (ClientData.NotifyingMerchants == null) return true;

            if (category == nameof(MerchantData.Zones))
            {
                return ClientData.NotifyingMerchants.FirstOrDefault(x => x.Name == name)?.Zones.Any(x => x == value as string) ?? false;
            }
            else if (category == nameof(MerchantData.Cards))
            {
                return ClientData.NotifyingMerchants.FirstOrDefault(x => x.Name == name)?.Cards.Any(x => x.Name == (value as Item)?.Name) ?? false;
            }
            else
            {
                throw new ArgumentException($"unknown {nameof(category)}.");
            }
        }

        protected bool HasNotifiedMerchantValues(string name)
        {
            if (ClientData == null) return false;
            if (ClientData.NotifyingMerchants == null) return false;

            return ClientData.NotifyingMerchants.Any(x => x.Name == name);
        }
    }
}
