using System.ComponentModel.DataAnnotations;

namespace WanderLost.Shared.Data;

[MessagePack.MessagePackObject]
public class CardNotification
{
    [MessagePack.IgnoreMember]
    public int PushSubscriptionId { get; init; }

    [MessagePack.Key(0)]
    [MaxLength(40)]
    public required string CardName { get; init; }
}