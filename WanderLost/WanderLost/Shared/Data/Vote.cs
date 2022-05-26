using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WanderLost.Shared.Data;

[MessagePack.MessagePackObject]
public class Vote
{
    [MessagePack.Key(0)]
    public Guid ActiveMerchantId { get; init; }

    [JsonIgnore]
    [MaxLength(60)]
    [MessagePack.IgnoreMember]
    public string ClientId { get; init; } = string.Empty;

    [JsonIgnore]
    [MaxLength(60)]
    [MessagePack.IgnoreMember]
    public string? UserId { get; init; }

    [MessagePack.Key(1)]
    public VoteType VoteType { get; set; }
}
