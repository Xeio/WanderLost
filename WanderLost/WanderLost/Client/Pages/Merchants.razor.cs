using Microsoft.AspNetCore.Components;
using WanderLost.Client.Services;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Pages
{
    public partial class Merchants : IAsyncDisposable
    {
        [Inject] public ClientSettingsController ClientSettings { get; init; } = default!;
        [Inject] public ClientStaticDataController StaticData { get; init; } = default!;
        [Inject] public MerchantHubClient HubClient { get; init; } = default!;
        [Inject] public ClientNotificationService Notifications { get; init; } = default!;
        [Inject] public NavigationManager NavigationManager { get; init; } = default!;
        [Inject] public ActiveDataController ActiveData { get; init; } = default!;
        [Inject] public IConfiguration Configuration { get; init; } = default!;

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
                    Task.Run(() => ClientSettings.SetRegion(_serverRegion ?? string.Empty));
                    Task.Run(() => UpdateMerchants(true));
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
                    Task.Run(() => ClientSettings.SetServer(_server ?? string.Empty));
                    Task.Run(() => ServerChanged(oldValue));
                }
            }
        }

        private Timer? _timer;
        private readonly List<IDisposable> _hubEvents = new();
        private bool _spawnNotified = true;

        protected override async Task OnInitializedAsync()
        {
            await StaticData.Init();
            await ClientSettings.Init();
            await ActiveData.Init();

            _timer = new Timer(TimerTick, null, 1, 1000);

            ServerRegion = ClientSettings.Region;
            Server = ClientSettings.Server;

            _hubEvents.Add(HubClient.OnUpdateMerchantGroup(async (server, serverMerchantGroup) =>
            {
                if (Server != server) return;
                
                if (ActiveData.MerchantGroups.FirstOrDefault(m => m.MerchantName == serverMerchantGroup.MerchantName) is ActiveMerchantGroup clientGroup)
                {
                    foreach (var merchant in serverMerchantGroup.ActiveMerchants)
                    {
                        if (ActiveData.MerchantDictionary.TryAdd(merchant.Id, merchant))
                        {
                            //Only need to notify/process new merchants
                            clientGroup.ActiveMerchants.Add(merchant);
                            await Notifications.CheckItemNotification(clientGroup);
                        }
                    }
                }

                await InvokeAsync(StateHasChanged);
            }));

            _hubEvents.Add(HubClient.OnUpdateVoteSelf(async (merchantId, voteType) =>
            {
                ActiveData.Votes[merchantId] = voteType;
                await InvokeAsync(StateHasChanged);
            }));

            _hubEvents.Add(HubClient.OnUpdateVoteTotal(async (merchantId, voteTotal) =>
            {
                if(ActiveData.MerchantDictionary.TryGetValue(merchantId, out var merchant))
                {
                    merchant.Votes = voteTotal;
                }
                if (ActiveData.MerchantGroups.FirstOrDefault(mg => mg.ActiveMerchants.Any(m => m.Id == merchantId)) is ActiveMerchantGroup merchantGroup)
                {
                    await Notifications.CheckItemNotification(merchantGroup);
                }

                await InvokeAsync(StateHasChanged);
            }));

            HubClient.HubConnection.Reconnected += HubConnection_Reconnected;

            await HubClient.Connect();

            await Notifications.ValidatePushSubscription(HubClient);
        }

        private async Task HubConnection_Reconnected(string? arg)
        {
            if (int.TryParse(Configuration["ClientVersion"], out var version))
            {
                if (await HubClient.HasNewerClient(version))
                {
                    //Force client to reload to match server
                    NavigationManager.NavigateTo("", true);
                    return;
                }
            }
            if (!string.IsNullOrWhiteSpace(Server))
            {
                await HubClient.SubscribeToServer(Server);
            }
            await SynchronizeServer();
        }

        public async ValueTask DisposeAsync()
        {
            if (_timer is not null)
            {
                await _timer.DisposeAsync();
            }

            foreach (var hubEvent in _hubEvents)
            {
                hubEvent.Dispose();
            }
            _hubEvents.Clear();

            HubClient.HubConnection.Reconnected -= HubConnection_Reconnected;

            await Notifications.ClearNotifications();

            GC.SuppressFinalize(this);
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
                await SynchronizeServer(true);
            }
        }

        private async Task SynchronizeServer(bool forceClear = false)
        {
            var serverMerchants = string.IsNullOrWhiteSpace(Server) || ActiveData.MerchantGroups.All(m => !m.IsActive) ?
                                        //Don't need to check server if no server or all merchants are inactive
                                        Enumerable.Empty<ActiveMerchantGroup>() :
                                        await HubClient.GetKnownActiveMerchantGroups(Server);

            if (forceClear)
            {
                await Notifications.ClearNotifications();
                foreach (var group in ActiveData.MerchantGroups)
                {
                    group.ClearInstances();
                }
            }

            if (!string.IsNullOrWhiteSpace(Server))
            {
                foreach (var serverMerchantGroup in serverMerchants)
                {
                    if (ActiveData.MerchantGroups.FirstOrDefault(mg => mg.MerchantName == serverMerchantGroup.MerchantName) is ActiveMerchantGroup clientGroup)
                    {
                        foreach (var merchant in serverMerchantGroup.ActiveMerchants)
                        {
                            if (ActiveData.MerchantDictionary.TryAdd(merchant.Id, merchant))
                            {
                                //Normally only want to add/notify newly discovered merchants
                                clientGroup.ActiveMerchants.Add(merchant);
                                await Notifications.CheckItemNotification(clientGroup);
                            }
                            else if(forceClear)
                            {
                                //...unless we force cleared the groups such as swapping servers
                                ActiveData.MerchantDictionary[merchant.Id] = merchant;
                                clientGroup.ActiveMerchants.Add(merchant);
                            }
                        }
                    }
                }

                if (ActiveData.MerchantDictionary.Count > 0)
                {
                    foreach (var vote in await HubClient.RequestClientVotes(Server))
                    {
                        ActiveData.Votes[vote.ActiveMerchantId] = vote.VoteType;
                    }
                }
            }

            await InvokeAsync(StateHasChanged);
        }

        async void TimerTick(object? _)
        {
            await UpdateMerchants();
            await InvokeAsync(StateHasChanged);
        }

        private async Task UpdateMerchants(bool force = false)
        {
            if (string.IsNullOrWhiteSpace(_serverRegion)) return;
            if (ActiveData.MerchantGroups.Count == 0) return;

            bool resort = false;

            if (!_spawnNotified && ActiveData.MerchantGroups.Any(mg => mg.IsActive))
            {
                await Notifications.CheckMerchantSpawnNotification(ActiveData.MerchantGroups.Where(mg => mg.IsActive));
                _spawnNotified = true;
            }

            foreach (var merchantGroup in ActiveData.MerchantGroups)
            {
                if (force || merchantGroup.AppearanceExpires < DateTimeOffset.UtcNow)
                {
                    merchantGroup.CalculateNextAppearance(StaticData.ServerRegions[_serverRegion].UtcOffset);
                    merchantGroup.ClearInstances();
                    ActiveData.MerchantDictionary.Clear();
                    ActiveData.Votes.Clear();
                    _spawnNotified = false;
                    resort = true;

                    //Clear previous notifications that are no longer relevant
                    await Notifications.ClearNotifications();
                }
            }

            if (resort)
            {
                if(ActiveData.MerchantGroups.Any(mg => mg.IsActive)) _spawnNotified = true;

                ActiveData.MerchantGroups.Sort((x, y) => {
                    var compare = x.NextAppearance.CompareTo(y.NextAppearance);
                    if(compare == 0)
                    {
                        compare = x.MerchantData.SortOrder.CompareTo(y.MerchantData.SortOrder);
                    }
                    return compare;
                });
            }
        }
    }
}
