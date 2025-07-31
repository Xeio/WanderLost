using HubClientSourceGenerator;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using WanderLost.Shared;
using WanderLost.Shared.Interfaces;

namespace WanderLost.Client.Services;

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
            .WithUrl(configuration["SocketEndpoint"] ?? throw new ApplicationException("Missing SocketEndpoint configuration"), options =>
            {
                options.SkipNegotiation = true;
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                options.AccessTokenProvider = GetToken;
            })
            .WithAutomaticReconnect([
                //Stagger reconnections a bit so server doesn't get hammered after a restart
                TimeSpan.FromSeconds(Random.Shared.Next(5,10)),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(2),
                TimeSpan.FromMinutes(4),
                TimeSpan.FromMinutes(8),
                TimeSpan.FromMinutes(16),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(30),
            ])
            .AddMessagePackProtocol(Utils.BuildMessagePackOptions)
            .Build();
        HubConnection.ServerTimeout = TimeSpan.FromMinutes(8);
        //Would like to increase this, but the browser seems to force-close "idle" sockets after ~3 minutes
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

    public async Task Connect()
    {
        if (HubConnection.State == HubConnectionState.Disconnected)
        {
            await HubConnection.StartAsync();
        }
    }
}
