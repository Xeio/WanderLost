using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WanderLost.Server.Discord.Data;

public class DiscordNotification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong UserId { get; set; }

    [MaxLength(20)]
    public string Server { get; set; } = string.Empty;

    public int CardVoteThreshold { get; set; }

    /// <summary>
    /// Flag to indicate if the subscription should be picked up in the next poll to send a test
    /// </summary>
    public bool SendTestNotification { get; set; }

    public bool CatalystNotification { get; set; }

    public ICollection<DiscordCardNotification> CardNotifications { get; init; } = [];

    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.Now;
}
