using Discord;
using Discord.WebSocket;

namespace WanderLost.Server.Discord;

public static class DiscordRegistrationExtensions
{
    public static void AddDiscord(this WebApplicationBuilder builder)
    {
        var discordBotToken = builder.Configuration["DiscordBotToken"];
        if (!string.IsNullOrWhiteSpace(discordBotToken))
        {
            builder.Services.AddSingleton<DiscordSocketClient>((provider) =>
            {
                var logger = provider.GetRequiredService<ILogger<DiscordSocketClient>>();
                logger.LogInformation("Starting and connecting discord client");

                return Task.Run(async () =>
                {
                    var readyCompletion = new TaskCompletionSource();
                    Task OnClientReady()
                    {
                        readyCompletion.SetResult();
                        return Task.CompletedTask;
                    }
                    var discordSocketConfig = new DiscordSocketConfig()
                    {
                        GatewayIntents =  GatewayIntents.None,
                    };
                    var client = new DiscordSocketClient(discordSocketConfig);
                    client.Ready += OnClientReady;
                    await client.LoginAsync(TokenType.Bot, discordBotToken);
                    await client.StartAsync();
                    await readyCompletion.Task;
                    client.Ready -= OnClientReady;
                    return client;
                }).Result;
            });

            builder.Services.AddHostedService<DiscordBotService>();

            builder.Services.AddScoped<DiscordSubscriptionManager>();
            builder.Services.AddScoped<DiscordPushProcessor>();
            builder.Services.AddScoped<IDiscordCommand, ManageNotificationsCommand>();
            builder.Services.AddScoped<IDiscordCommand, SendTestNotificationCommand>();
        }
    }    
}