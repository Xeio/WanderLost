using Microsoft.AspNetCore.SignalR.Client;
using WanderLost.Shared.Interfaces;
using HubClientSourceGenerator;
using WanderLost.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace WanderLost.Client.Services
{
    [AutoHubClient(typeof(IMerchantHubClient))]
    [AutoHubServer(typeof(IMerchantHubServer))]
    public sealed partial class MerchantHubClient : IAsyncDisposable
    {
        public HubConnection HubConnection { get; init; }
        private readonly IAccessTokenProvider _accessTokenProvider;

        public MerchantHubClient(IConfiguration configuration, IAccessTokenProvider accessTokenProvider)
        {
            _accessTokenProvider = accessTokenProvider;
            HubConnection = new HubConnectionBuilder()
                .WithUrl(configuration["SocketEndpoint"], options => { 
                    options.SkipNegotiation = true;
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                    options.AccessTokenProvider = GetToken;
                })
                .WithAutomaticReconnect(new[] {
                    //Stargger reconnections a bit so server doesn't get hammered after a restart
                    TimeSpan.FromSeconds(Random.Shared.Next(5,120)),
                    TimeSpan.FromSeconds(Random.Shared.Next(10,120)),
                    TimeSpan.FromSeconds(Random.Shared.Next(30,120)),
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(5),
                })
                .AddMessagePackProtocol(Utils.BuildMessagePackOptions)
                .Build();
            HubConnection.ServerTimeout = TimeSpan.FromMinutes(2);
            HubConnection.KeepAliveInterval = TimeSpan.FromMinutes(1);
        }
        
        private async Task<string?> GetToken()
        {
            var tokenResult = await _accessTokenProvider.RequestAccessToken();
            tokenResult.TryGetToken(out var token);
            return token?.Value;
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
