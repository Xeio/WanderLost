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

                Task LogDiscordClientMessage(LogMessage arg)
                {
                    var level = LogLevel.Trace;
                    switch (arg.Severity)
                    {
                        case LogSeverity.Warning: level = LogLevel.Warning; break;
                        case LogSeverity.Critical: level = LogLevel.Critical; break;
                        case LogSeverity.Error: level = LogLevel.Error; break;
                        case LogSeverity.Info: level = LogLevel.Information; break;
                        case LogSeverity.Debug: level = LogLevel.Debug; break;
                        case LogSeverity.Verbose: level = LogLevel.Trace; break;
                    }
                    if (arg.Exception != null)
                    {
                        logger.LogError(arg.Exception, "{Message}", arg.Message);
                    }
                    else
                    {
                        logger.Log(level, "{Message}", arg.Message);
                    }
                    return Task.CompletedTask;
                }

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
                        GatewayIntents = GatewayIntents.DirectMessages,
                    };
                    var client = new DiscordSocketClient(discordSocketConfig);
                    client.Log += LogDiscordClientMessage;
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