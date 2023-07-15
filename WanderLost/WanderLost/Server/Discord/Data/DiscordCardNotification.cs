using System.ComponentModel.DataAnnotations;

namespace WanderLost.Server.Discord.Data;

public class DiscordCardNotification
{
    public ulong DiscordNotificationUserId { get; init; }

    [MaxLength(40)]
    public required string CardName { get; init; }
}
