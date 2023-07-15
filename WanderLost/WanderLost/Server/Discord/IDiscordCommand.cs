using Discord;
using Discord.WebSocket;

namespace WanderLost.Server.Discord;

public interface IDiscordCommand
{
    public SlashCommandProperties CreateCommand();
    public Task ModalSubmitted(SocketModal arg);
    public Task ButtonExecuted(SocketMessageComponent arg);
    public Task SlashCommandExecuted(SocketSlashCommand arg);
    public Task SelectMenuExecuted(SocketMessageComponent arg);
}
