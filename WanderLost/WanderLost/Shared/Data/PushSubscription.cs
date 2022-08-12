using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WanderLost.Shared.Data;

[MessagePack.MessagePackObject]
public class PushSubscription
{
    /// <summary>
    /// The Firebase Cloud Messaging client token
    /// </summary>
    [Key]
    [MessagePack.Key(0)]
    public string Token { get; set; } = string.Empty;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public int Id { get; set; }

    [MessagePack.Key(1)]
    public string Server { get; set; } = string.Empty;

    [MessagePack.Key(2)]
    public int WeiVoteThreshold { get; set; }

    [MessagePack.Key(3)]
    public bool WeiNotify { get; set; }

    [MessagePack.Key(4)]
    public int RapportVoteThreshold { get; set; }

    [MessagePack.Key(5)]
    public bool LegendaryRapportNotify { get; set; }

    /// <summary>
    /// Flag to indicate if the subscription should be picked up in the next poll to send a test
    /// </summary>
    [MessagePack.Key(6)]
    public bool SendTestNotification { get; set; }

    /// <summary>
    /// Number of consecutive messages that failed for Firebase, may be used to clean up defunct subscriptions.
    /// This automatically clears when a user updates their subscription.
    /// </summary>
    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public int ConsecutiveFailures { get; set; }

    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.Now;
}
