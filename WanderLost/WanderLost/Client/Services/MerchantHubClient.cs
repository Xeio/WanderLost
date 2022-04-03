using Microsoft.AspNetCore.SignalR.Client;
using WanderLost.Shared.Data;
using WanderLost.Shared.Interfaces;

namespace WanderLost.Client.Services
{
    //Implements client for incoming calls, and server to act as a proxy for outgoing calls
    public class MerchantHubClient : IMerchantHubServer, IMerchantHubClient, IAsyncDisposable
    {
        public HubConnection HubConnection {get; init; }

        public MerchantHubClient(IConfiguration configuration)
        {
            HubConnection = new HubConnectionBuilder()
                .WithUrl(configuration["SocketEndpoint"])
                .WithAutomaticReconnect(new[] {
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromMinutes(1), 
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(5),
                })
                .Build();
            HubConnection.ServerTimeout = TimeSpan.FromMinutes(2);
            HubConnection.KeepAliveInterval = TimeSpan.FromMinutes(1);
        }

        public async ValueTask DisposeAsync()
        {
            if(HubConnection is not null)
            {
                await HubConnection.DisposeAsync();
            }
            GC.SuppressFinalize(this);
        }

        public async Task SubscribeToServer(string server)
        {
            await HubConnection.SendAsync(nameof(SubscribeToServer), server);
        }

        public async Task UnsubscribeFromServer(string server)
        {
            await HubConnection.SendAsync(nameof(UnsubscribeFromServer), server);
        }

        public async Task UpdateMerchant(string server, ActiveMerchant merchant)
        {
            await HubConnection.SendAsync(nameof(UpdateMerchant), server, merchant);
        }

        Task IMerchantHubClient.UpdateMerchantGroup(string server, ActiveMerchantGroup merchantGroup)
        {
            //Not a callable server method
            throw new NotImplementedException();
        }

        public IDisposable OnUpdateMerchant(Action<string, ActiveMerchant> action)
        {
            return HubConnection.On(nameof(UpdateMerchant), action);
        }

        public IDisposable OnUpdateMerchantGroup(Action<string, ActiveMerchantGroup> action)
        {
            return HubConnection.On(nameof(IMerchantHubClient.UpdateMerchantGroup), action);
        }

        public async Task<IEnumerable<ActiveMerchantGroup>> GetKnownActiveMerchantGroups(string server)
        {
            return await HubConnection.InvokeAsync<IEnumerable<ActiveMerchantGroup>>(nameof(GetKnownActiveMerchantGroups), server);
        }

        public async Task Vote(string server, Guid merchantId, VoteType voteType)
        {
            await HubConnection.SendAsync(nameof(Vote), server, merchantId, voteType);
        }

        public Task UpdateVoteTotal(Guid merchantId, int voteTotal)
        {
            //Todo: Maybe this class should be constructed by a code generator so we don't need these sorts of methods
            //There's a repeated pattern of an "On" method for server calls, and a SendAsync/InvokeAsync for client calls
            throw new NotImplementedException();
        }

        public delegate void UpdateVoteTotalHandler(Guid merchantId, int voteTotal);
        public IDisposable OnUpdateVoteTotal(UpdateVoteTotalHandler handler)
        {
            var action = new Action<Guid, int>(handler);
            return HubConnection.On(nameof(UpdateVoteTotal), action);
        }
    }
}
