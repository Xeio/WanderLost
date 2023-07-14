using Discord.WebSocket;

namespace WanderLost.Server.Discord;

public class DiscordBotService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly DiscordSocketClient _discordClient;

    public DiscordBotService(ILogger<DiscordBotService> logger, IServiceProvider services, DiscordSocketClient discordClient)
    {
        _logger = logger;
        _services = services;
        _discordClient = discordClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Initializing discord commands");

        //var x = await _discordClient.GetGlobalApplicationCommandsAsync();
        //foreach(var command in x)
        //{
        //    await command.DeleteAsync();
        //}
        await ActivatorUtilities.CreateInstance<ManageNotificationsCommand>(_services).Init();

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
