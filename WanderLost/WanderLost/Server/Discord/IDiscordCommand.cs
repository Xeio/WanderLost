using Discord.WebSocket;

namespace WanderLost.Server.Discord;

public interface IDiscordCommand
{
    public Task CreateCommand();
    public Task ModalSubmitted(SocketModal arg);
    public Task ButtonExecuted(SocketMessageComponent arg);
    public Task SlashCommandExecuted(SocketSlashCommand arg);
    public Task SelectMenuExecuted(SocketMessageComponent arg);
}
