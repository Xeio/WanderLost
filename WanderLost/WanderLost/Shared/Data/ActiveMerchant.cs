using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WanderLost.Shared.Data;

[MessagePack.MessagePackObject]
public class ActiveMerchant
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    [MessagePack.Key(0)]
    public Guid Id { get; set; }

    [MaxLength(20)]
    [MessagePack.Key(1)]
    public string Name { get; init; } = string.Empty;

    [MessagePack.Key(3)]
    public List<Item> Cards { get; set; } = [];

    [MessagePack.Key(4)]
    public List<Item> Rapports { get; set; } = [];

    [MessagePack.Key(5)]
    public int Votes { get; set; }

    [MaxLength(50)]
    [MessagePack.Key(6)]
    public string? Tradeskill { get; set; }

    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public List<Vote> ClientVotes { get; set; } = [];

    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public bool Hidden { get; set; }

    /// <summary>
    /// Identifier for client on the server
    /// </summary>
    [MaxLength(60)]
    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public string UploadedBy { get; set; } = string.Empty;

    [MaxLength(60)]
    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public string? UploadedByUserId { get; set; }

    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public bool IsRareCombination
    {
        get
        {
            return Cards.Any(c => c.Rarity >= Rarity.Legendary) || Rapports.Any(r => r.Rarity >= Rarity.Legendary);
        }
    }

    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public int? ActiveMerchantGroupId { get; set; }

    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public ActiveMerchantGroup ActiveMerchantGroup { get; set; } = new();

    /// <summary>
    /// Push message processing flag
    /// </summary>
    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public bool RequiresProcessing { get; set; }

    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public bool RequiresVoteProcessing { get; set; }

    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public bool PostProcessComplete { get; set; }

    [JsonIgnore]
    [MessagePack.IgnoreMember]
    public bool RequiresLeaderboardProcessing { get; set; } = true;

    public bool IsValid(Dictionary<string, MerchantData> allMerchantData)
    {
        if (string.IsNullOrWhiteSpace(Name) ||
            Cards.Count == 0 || 
            Rapports.Count == 0)
        {
            return false;
        }

        if (!allMerchantData.TryGetValue(Name, out var data)) return false;

        if (!Cards.All(data.Cards.Contains) ||
            !Rapports.All(data.Rapports.Contains))
        {
            return false;
        }

        if (string.IsNullOrEmpty(Tradeskill) && data.Tradeskills.Count > 0) return false;

        if (Tradeskill is not null)
        {
            if (data.Tradeskills.Count == 0) return false;
            if (!data.Tradeskills.Contains(Tradeskill)) return false;
        }

        return true;
    }

    public bool IsEqualTo(ActiveMerchant merchant)
    {
        return Name == merchant.Name &&
            Enumerable.SequenceEqual(Cards.Order(), merchant.Cards.Order()) &&
            Enumerable.SequenceEqual(Rapports.Order(), merchant.Rapports.Order()) &&
            Tradeskill == merchant.Tradeskill;
    }
}
