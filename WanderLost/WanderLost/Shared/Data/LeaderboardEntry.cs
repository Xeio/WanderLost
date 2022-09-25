using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WanderLost.Shared.Data;

[MessagePack.MessagePackObject]
public class LeaderboardEntry
{
    [Key]
    [MaxLength(60)]
    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(40)]
    [MessagePack.Key(0)]
    public string DisplayName { get; set; } = string.Empty;

    [MessagePack.Key(1)]
    public int TotalVotes { get; set; }

    [MessagePack.Key(2)]
    public int TotalSubmissions { get; set; }

    [MessagePack.Key(3)]
    public string PrimaryServer { get; set; } = string.Empty;
}
