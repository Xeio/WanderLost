﻿using Microsoft.AspNetCore.SignalR.Client;
using WanderLost.Shared.Data;
using WanderLost.Shared.Interfaces;
using HubClientSourceGenerator;

namespace WanderLost.Client.Services
{
    //Implements client for incoming calls, and server to act as a proxy for outgoing calls
    [AutoHubClient(typeof(IMerchantHubClient))]
    //[AutoHubClient(typeof(IMerchantHubClient))]
    public partial class MerchantHubClient : IMerchantHubServer, IAsyncDisposable
    {
        public HubConnection HubConnection { get; init; }

        public MerchantHubClient(IConfiguration configuration)
        {
            HubConnection = new HubConnectionBuilder()
                .WithUrl(configuration["SocketEndpoint"], options => { 
                    options.SkipNegotiation = true;
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                })
                .WithAutomaticReconnect(new[] {
                    //Stargger reconnections a bit so server doesn't get hammered after a restart
                    TimeSpan.FromSeconds(Random.Shared.Next(5,120)),
                    TimeSpan.FromSeconds(Random.Shared.Next(10,120)),
                    TimeSpan.FromSeconds(Random.Shared.Next(30,120)),
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(5),
                })
                .Build();
            HubConnection.ServerTimeout = TimeSpan.FromMinutes(2);
            HubConnection.KeepAliveInterval = TimeSpan.FromMinutes(1);
        }

        public async ValueTask DisposeAsync()
        {
            if (HubConnection is not null)
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

        public Task UpdateVoteSelf(Guid merchantId, VoteType voteType)
        {
            throw new NotImplementedException();
        }

        public delegate void UpdateVoteSelfHandler(Guid merchantId, VoteType voteType);
        public IDisposable OnUpdateVoteSelf(UpdateVoteSelfHandler handler)
        {
            var action = new Action<Guid, VoteType>(handler);
            return HubConnection.On(nameof(UpdateVoteSelf), action);
        }

        public async Task<bool> HasNewerClient(int version)
        {
            return await HubConnection.InvokeAsync<bool>(nameof(HasNewerClient), version);
        }

        public async Task RequestClientVotes(string server)
        {
            await HubConnection.InvokeAsync<IEnumerable<Vote>>(nameof(RequestClientVotes), server);
        }
    }
}
