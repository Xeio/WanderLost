using Discord;
using Discord.WebSocket;
using WanderLost.Server.Controllers;

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
        var guild = _discordClient.Guilds.FirstOrDefault();

        var commandBuilder = new SlashCommandBuilder();
        commandBuilder.WithName("merchant-notify");
        commandBuilder.WithDescription("Test notification command");
        commandBuilder.WithDMPermission(true);

        var command = await _discordClient.CreateGlobalApplicationCommandAsync(commandBuilder.Build());

        _discordClient.SlashCommandExecuted += _discordClient_SlashCommandExecuted;
        _discordClient.SelectMenuExecuted += _discordClient_SelectMenuExecuted;

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }


    private async Task _discordClient_SlashCommandExecuted(SocketSlashCommand arg)
    {
        using var scope = _services.CreateScope();
        var dataController = scope.ServiceProvider.GetRequiredService<DataController>();
        var serverRegions = await dataController.GetServerRegions();

        var select = new SelectMenuBuilder();
        select.WithPlaceholder("Select server region");
        select.WithCustomId("select-region");
        foreach(var server in serverRegions)
        {
            select.AddOption(server.Value.Name, server.Key);
        }

        var builder = new ComponentBuilder().WithSelectMenu(select);

        await arg.RespondAsync("Server Region", components: builder.Build(), ephemeral: true);
    }

    private async Task _discordClient_SelectMenuExecuted(SocketMessageComponent arg)
    {
        using var scope = _services.CreateScope();
        var dataController = scope.ServiceProvider.GetRequiredService<DataController>();
        var serverRegions = await dataController.GetServerRegions();

        switch (arg.Data.CustomId)
        {
             case "select-region":
                var val = arg.Data.Values.First();
                var select = new SelectMenuBuilder();
                select.WithPlaceholder("Select server");
                select.WithCustomId("select-server");
                foreach (var server in serverRegions[val].Servers)
                {
                    select.AddOption(server, server);
                }

                var builder = new ComponentBuilder().WithSelectMenu(select);

                await arg.RespondAsync("Server", components: builder.Build(), ephemeral: true);

                break;
            case "select-server":
                await arg.RespondAsync("Blah", ephemeral: true);
                await Task.Delay(10_000);
                await arg.User.SendMessageAsync("This is a follow up");
                break;
        }
    }
}
