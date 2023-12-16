using Discord;
using Discord.WebSocket;
using System.Diagnostics.Metrics;

namespace WanderLost.Server.Discord;

public class DiscordBotService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly DiscordSocketClient _discordClient;
    private readonly Counter<int> _interactionsCounter;

    public DiscordBotService(ILogger<DiscordBotService> logger, IServiceProvider services, DiscordSocketClient discordClient, IMeterFactory meterFactory)
    {
        _logger = logger;
        _services = services;
        _discordClient = discordClient;

        var meter = meterFactory.Create("LostMerchants");
        _interactionsCounter = meter.CreateCounter<int>("discord.interactions");
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
            _interactionsCounter.Add(1, new KeyValuePair<string, object?>("interactionType", nameof(MessageReceived)));
            await userMessage.ReplyAsync($"To set up a subscription use the {Format.Code("/manage-merchant-notifications")} command.");
        }
    }

    private async Task ModalSubmitted(SocketModal arg)
    {
        _interactionsCounter.Add(1, new KeyValuePair<string, object?>("interactionType", nameof(ModalSubmitted)));
        using var scope = _services.CreateScope();
        foreach (var command in scope.ServiceProvider.GetServices<IDiscordCommand>())
        {
            await command.ModalSubmitted(arg);
        }
    }

    private async Task ButtonExecuted(SocketMessageComponent arg)
    {
        _interactionsCounter.Add(1, new KeyValuePair<string, object?>("interactionType", nameof(ButtonExecuted)));
        using var scope = _services.CreateScope();
        foreach (var command in scope.ServiceProvider.GetServices<IDiscordCommand>())
        {
            await command.ButtonExecuted(arg);
        }
    }

    private async Task SlashCommandExecuted(SocketSlashCommand arg)
    {
        _interactionsCounter.Add(1, new KeyValuePair<string, object?>("interactionType", nameof(SlashCommandExecuted)));
        using var scope = _services.CreateScope();
        foreach (var command in scope.ServiceProvider.GetServices<IDiscordCommand>())
        {
            await command.SlashCommandExecuted(arg);
        }
    }

    private async Task SelectMenuExecuted(SocketMessageComponent arg)
    {
        _interactionsCounter.Add(1, new KeyValuePair<string, object?>("interactionType", nameof(SelectMenuExecuted)));
        using var scope = _services.CreateScope();
        foreach (var command in scope.ServiceProvider.GetServices<IDiscordCommand>())
        {
            await command.SelectMenuExecuted(arg);
        }
    }
}
