using Microsoft.AspNetCore.SignalR.Client;
using WanderLost.Shared.Interfaces;
using HubClientSourceGenerator;

namespace WanderLost.Client.Services
{
    [AutoHubClient(typeof(IMerchantHubClient))]
    [AutoHubServer(typeof(IMerchantHubServer))]
    public sealed partial class MerchantHubClient : IAsyncDisposable
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
        }
    }
}
