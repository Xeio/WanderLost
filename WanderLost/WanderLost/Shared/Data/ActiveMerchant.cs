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

        public Item Rapport { get; set; } = new();

        public int Votes { get; set; }

        [JsonIgnore]
        public List<Vote> ClientVotes { get; set; } = new();

        [JsonIgnore]
        public bool Hidden { get; set; }

        /// <summary>
        /// Identifier for client on the server, may be IP or later an account identifier
        /// </summary>
        [MaxLength(60)]
        [JsonIgnore]
        public string UploadedBy { get;set; } = string.Empty;

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
