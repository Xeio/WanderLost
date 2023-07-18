using Discord;
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
        _logger.LogInformation("Initializing discord bot service");

        _discordClient.SlashCommandExecuted += SlashCommandExecuted;
        _discordClient.SelectMenuExecuted += SelectMenuExecuted;
        _discordClient.ButtonExecuted += ButtonExecuted;
        _discordClient.ModalSubmitted += ModalSubmitted;
        _discordClient.MessageReceived += MessageReceived;

        using (var scope = _services.CreateScope())
        {
            var commands = new List<SlashCommandProperties>();
            foreach (var command in scope.ServiceProvider.GetServices<IDiscordCommand>())
            {
                commands.Add(command.CreateCommand());
            }
            if (commands.Count > 0)
            {
                await _discordClient.BulkOverwriteGlobalApplicationCommandsAsync(commands.ToArray());
            }
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task MessageReceived(SocketMessage arg)
    {
        if (arg.Type == MessageType.Default
            && !arg.Author.IsBot
            && arg is SocketUserMessage userMessage
            && arg.Channel is IDMChannel)
        {
            await userMessage.ReplyAsync($"To set up a subscription use the {Format.Code("/manage-merchant-notifications")} command.");
        }
    }

    private async Task ModalSubmitted(SocketModal arg)
    {
        using var scope = _services.CreateScope();
        foreach (var command in scope.ServiceProvider.GetServices<IDiscordCommand>())
        {
            await command.ModalSubmitted(arg);
        }
    }

    private async Task ButtonExecuted(SocketMessageComponent arg)
    {
        using var scope = _services.CreateScope();
        foreach (var command in scope.ServiceProvider.GetServices<IDiscordCommand>())
        {
            await command.ButtonExecuted(arg);
        }
    }

    private async Task SlashCommandExecuted(SocketSlashCommand arg)
    {
        using var scope = _services.CreateScope();
        foreach (var command in scope.ServiceProvider.GetServices<IDiscordCommand>())
        {
            await command.SlashCommandExecuted(arg);
        }
    }

    private async Task SelectMenuExecuted(SocketMessageComponent arg)
    {
        using var scope = _services.CreateScope();
        foreach (var command in scope.ServiceProvider.GetServices<IDiscordCommand>())
        {
            await command.SelectMenuExecuted(arg);
        }
    }
}
