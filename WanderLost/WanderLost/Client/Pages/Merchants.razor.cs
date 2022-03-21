﻿using Append.Blazor.Notifications;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Pages
{
    public partial class Merchants : IAsyncDisposable
    {
        [Inject] public ILocalStorageService LocalStorage { get; set; } = default!; //default! to suppress NULL warning
        [Inject] public ClientStaticDataController StaticData { get; set; } = default!; //default! to suppress NULL warning
        [Inject] public MerchantHubClient HubClient { get; set; } = default!; //default! to suppress NULL warning
        [Inject] public ClientNotificationService Notifications { get; set; } = default!; //default! to suppress NULL warning

        private string? _serverRegion;
        private string? ServerRegion
        {
            get { return _serverRegion; }
            set
            {
                if (_serverRegion != value)
                {
                    _serverRegion = value;
                    Server = null;
                    ServerRegionChanged();
                }
            }
        }

        private string? _server;
        private string? Server
        {
            get { return _server; }
            set
            {
                if (_server != value)
                {
                    var oldValue = _server;
                    _server = value;
                    Task.Run(SaveData);
                    Task.Run(() => ServerChanged(oldValue));
                }
            }
        }

        private List<ActiveMerchantGroup> _activeMerchantGroups = new();
        private Timer? _timer;

        protected override async Task OnInitializedAsync()
        {
            _ = Notifications.Init();
            await StaticData.Init();

            _activeMerchantGroups = StaticData.Merchants.Values.Select(m => new ActiveMerchantGroup() { MerchantData = m }).ToList();

            _timer = new Timer(TimerTick, null, 1, 1000);

            var savedData = await LocalStorage.GetItemAsync<ClientData?>(nameof(ClientData));
            ServerRegion = savedData?.Region;
            Server = savedData?.Server;

            HubClient.OnUpdateMerchantGroup((server, merchantGroup) =>
            {
                if (Server != server) return;
                if (_activeMerchantGroups.FirstOrDefault(m => m.MerchantName == merchantGroup.MerchantName) is ActiveMerchantGroup existing)
                {
                    if (merchantGroup.HasDifferentMerchantsTo(existing) && merchantGroup.ActiveMerchants.Any())
                    {
                        Notifications.CreateMerchantFoundNotification(merchantGroup);
                    }
                    else if (!merchantGroup.ActiveMerchants.Any())
                    {

                    }

                    existing.ReplaceInstances(merchantGroup.ActiveMerchants);
                }
                InvokeAsync(StateHasChanged);
            });

            if (HubClient.HubConnection.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected)
            {
                await HubClient.HubConnection.StartAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_timer is not null)
            {
                await _timer.DisposeAsync();
            }
        }

        private async Task ServerChanged(string? oldServer)
        {
            if (!string.IsNullOrWhiteSpace(oldServer))
            {
                await HubClient.UnsubscribeFromServer(oldServer);
            }
            if (!string.IsNullOrWhiteSpace(Server) && !string.IsNullOrWhiteSpace(ServerRegion))
            {
                await HubClient.SubscribeToServer(Server);

                //Sync with the server's current data
                _activeMerchantGroups.ForEach(m => m.ClearInstances());
                foreach (var serverMerchantGroup in await HubClient.GetKnownActiveMerchantGroups(Server))
                {
                    serverMerchantGroup.CalculateNextAppearance(StaticData.ServerRegions[ServerRegion].UtcOffset);
                    if (_activeMerchantGroups.FirstOrDefault(mg => mg.MerchantName == serverMerchantGroup.MerchantName) is ActiveMerchantGroup existing)
                    {
                        existing.ReplaceInstances(serverMerchantGroup.ActiveMerchants);
                    }
                }
                StateHasChanged();
            }
        }

        private void ServerRegionChanged()
        {
            UpdateMerchants(true);
        }

        private async Task SaveData()
        {
            await LocalStorage.SetItemAsync(nameof(ClientData), new ClientData()
            {
                Region = ServerRegion ?? string.Empty,
                Server = Server ?? string.Empty,
            });
        }

        async void TimerTick(object? _)
        {
            UpdateMerchants();
            await InvokeAsync(StateHasChanged);
        }

        private void UpdateMerchants(bool force = false)
        {
            if (string.IsNullOrWhiteSpace(_serverRegion)) return;
            if (_activeMerchantGroups.Count == 0) return;

            bool resort = false;

            foreach (var merchantGroup in _activeMerchantGroups)
            {
                if (force || merchantGroup.AppearanceExpires < DateTimeOffset.UtcNow)
                {
                    merchantGroup.CalculateNextAppearance(StaticData.ServerRegions[_serverRegion].UtcOffset);
                    merchantGroup.ClearInstances();
                    resort = true;
                }
            }

            if (resort)
            {
                _activeMerchantGroups = _activeMerchantGroups.OrderBy(m => m.NextAppearance).ThenBy(m => m.MerchantData.Region).ToList();

                if (!force)
                {
                    if (_activeMerchantGroups.FirstOrDefault(x => x.IsActive) is ActiveMerchantGroup newActiveMerchantGroup)
                    {
                        Notifications.CreateMerchantSpawnNotification(newActiveMerchantGroup);
                    }
                }
            }
        }
    }
}
