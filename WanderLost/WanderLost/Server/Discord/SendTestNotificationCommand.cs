using Discord;
using Discord.WebSocket;

namespace WanderLost.Server.Discord
{
    public class SendTestNotificationCommand : IDiscordCommand
    {
        private readonly DiscordSubscriptionManager _subscriptionManager;
        private readonly ILogger<SendTestNotificationCommand> _logger;

        const string SEND_TEST_NOTIFICATION_COMMAND = "send-test-notification";

        public SendTestNotificationCommand(ILogger<SendTestNotificationCommand> logger, DiscordSubscriptionManager subscriptionManager)
        {
            _logger = logger;
            _subscriptionManager = subscriptionManager;
        }

        public SlashCommandProperties CreateCommand()
        {
            var commandBuilder = new SlashCommandBuilder()
            {
                Name = SEND_TEST_NOTIFICATION_COMMAND,
                Description = "Send a test notification from Lost Merchants",
                IsDMEnabled = true,
            };

            return commandBuilder.Build();
        }

        public async Task SlashCommandExecuted(SocketSlashCommand arg)
        {
            if (arg.CommandName == SEND_TEST_NOTIFICATION_COMMAND)
            {
                await _subscriptionManager.SetSubscriptionTestFlag(arg.User.Id);

                await arg.RespondAsync("Test notification should be sent in the next 30 seconds. If you don't receive a message the bot is unable to DM you.", ephemeral: true);
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
