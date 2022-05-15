using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WanderLost.Shared.Data
{
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

        [MaxLength(40)]
        [MessagePack.Key(2)]
        public string Zone { get; set; } = string.Empty;

        [MessagePack.Key(3)]
        public Item Card { get; set; } = new();

        [MessagePack.Key(4)]
        public Item Rapport { get; set; } = new();

        [MessagePack.Key(5)]
        public int Votes { get; set; }

        [JsonIgnore]
        [MessagePack.IgnoreMember]
        public List<Vote> ClientVotes { get; set; } = new();

        [JsonIgnore]
        [MessagePack.IgnoreMember]
        public bool Hidden { get; set; }

        /// <summary>
        /// Identifier for client on the server
        /// </summary>
        [MaxLength(60)]
        [JsonIgnore]
        [MessagePack.IgnoreMember]
        public string UploadedBy { get;set; } = string.Empty;

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
                return Card.Rarity >= Rarity.Legendary || Rapport.Rarity >= Rarity.Legendary;
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

        public void ClearInstance()
        {
            Zone = string.Empty;
            Card = new();
            Rapport = new();
        }

        public void CopyInstance(ActiveMerchant other)
        {
            //Copies only data sent between client and server
            Zone = other.Zone;
            Card = other.Card;
            Rapport = other.Rapport;
        }

        public bool IsValid(Dictionary<string, MerchantData> allMerchantData)
        {
            if (string.IsNullOrWhiteSpace(Name) ||
                string.IsNullOrWhiteSpace(Zone))
            {
                return false;
            }

            if (!allMerchantData.TryGetValue(Name, out var data)) return false;

            return data.Zones.Contains(Zone) &&
                    data.Cards.Contains(Card) &&
                    data.Rapports.Contains(Rapport);
        }

        public bool IsEqualTo(ActiveMerchant merchant)
        {
            return Name == merchant.Name &&
                Zone == merchant.Zone &&
                Card.Equals(merchant.Card) &&
                Rapport.Equals(merchant.Rapport);
        }
    }
}
