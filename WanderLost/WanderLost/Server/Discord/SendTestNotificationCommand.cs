using Discord;
using Discord.WebSocket;

namespace WanderLost.Server.Discord
{
    public class SendTestNotificationCommand(DiscordSubscriptionManager _subscriptionManager) : IDiscordCommand
    {
        const string SEND_TEST_NOTIFICATION_COMMAND = "send-test-notification";

        public SlashCommandProperties CreateCommand()
        {
            var commandBuilder = new SlashCommandBuilder()
            {
                Name = SEND_TEST_NOTIFICATION_COMMAND,
                Description = "Send a test notification from Lost Merchants",
                ContextTypes = [InteractionContextType.BotDm, InteractionContextType.Guild],
            };

            return commandBuilder.Build();
        }

        public async Task SlashCommandExecuted(SocketSlashCommand arg)
        {
            if (arg.CommandName == SEND_TEST_NOTIFICATION_COMMAND)
            {
                await arg.DeferAsync(ephemeral: true);

                await _subscriptionManager.SetSubscriptionTestFlag(arg.User.Id);

                await arg.FollowupAsync("Test notification should be sent in the next 30 seconds. If you don't receive a message the bot is unable to DM you.", ephemeral: true);
            }
        }

        public Task ButtonExecuted(SocketMessageComponent arg)
        {
            return Task.CompletedTask;
        }

        public Task ModalSubmitted(SocketModal arg)
        {
            return Task.CompletedTask;
        }

        public Task SelectMenuExecuted(SocketMessageComponent arg)
        {
            return Task.CompletedTask;
        }
    }
}
