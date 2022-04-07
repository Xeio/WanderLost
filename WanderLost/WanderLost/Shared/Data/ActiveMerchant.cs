using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WanderLost.Shared.Data
{
    public class ActiveMerchant
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }

        [MaxLength(20)]
        public string Name { get; init; } = string.Empty;

        [MaxLength(40)]
        public string Zone { get; set; } = string.Empty;

        public Item Card { get; set; } = new();

        public Rarity? RapportRarity { get; set; }

        public int Votes { get; set; }

        [JsonIgnore]
        public List<Vote> ClientVotes { get; set; } = new();

        /// <summary>
        /// Identifier for client on the server, may be IP or later an account identifier
        /// </summary>
        [JsonIgnore]
        public string UploadedBy { get;set; } = string.Empty;

        public void ClearInstance()
        {
            Zone = string.Empty;
            Card = new();
            RapportRarity = null;
        }

        public void CopyInstance(ActiveMerchant other)
        {
            //Copies only data sent between client and server
            Zone = other.Zone;
            Card = other.Card;
            RapportRarity = other.RapportRarity;
        }

        public bool IsValid(Dictionary<string, MerchantData> allMerchantData)
        {
            if (string.IsNullOrWhiteSpace(Name) ||
                string.IsNullOrWhiteSpace(Zone) ||
                RapportRarity is null)
            {
                return false;
            }

            if (!allMerchantData.ContainsKey(Name)) return false;

            var data = allMerchantData[Name];

            return data.Zones.Contains(Zone) &&
                    data.Cards.Any(c => c.Name == Card.Name && c.Rarity == Card.Rarity);
        }

        public bool IsEqualTo(ActiveMerchant merchant)
        {
            return Name == merchant.Name &&
                Zone == merchant.Zone &&
                Card.Name == merchant.Card.Name &&
                Card.Rarity == merchant.Card.Rarity &&
                RapportRarity == merchant.RapportRarity;
        }
    }
}
