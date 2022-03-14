using System.Text.Json.Serialization;

namespace WanderLost.Shared
{
    public class ActiveMerchant
    {
        public string Name { get; init; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public Item Card { get; set; } = new();
        public Rarity? RapportRarity { get; set; }

        [JsonIgnore]
        public DateTimeOffset NextAppearance { get; private set; }
        [JsonIgnore]
        public DateTimeOffset AppearanceExpires { get; private set; }

        public bool IsActive => DateTimeOffset.UtcNow > NextAppearance && DateTimeOffset.UtcNow < AppearanceExpires;

        public void CalculateNextAppearance(Dictionary<string, MerchantData> merchants, TimeSpan serverUtcOffset)
        {
            var expiresAfter = TimeSpan.FromMinutes(25);

            var nextAppearanceTime = merchants[Name].AppearanceTimes
                .Select(apperance => new DateTimeOffset(DateTimeOffset.UtcNow.ToOffset(serverUtcOffset).Date, serverUtcOffset) + apperance)
                .Where(time => time >= DateTimeOffset.UtcNow - expiresAfter)
                .FirstOrDefault();
            
            if(nextAppearanceTime == default)
            {
                //Next apperance is the following day
                nextAppearanceTime = merchants[Name].AppearanceTimes
                    .Select(apperance => new DateTimeOffset(DateTimeOffset.UtcNow.ToOffset(serverUtcOffset).Date.AddDays(1), serverUtcOffset) + apperance)
                    .Where(time => time >= DateTimeOffset.UtcNow - expiresAfter)
                    .FirstOrDefault();
            }

            NextAppearance = nextAppearanceTime;
            AppearanceExpires = nextAppearanceTime + expiresAfter;
        }

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
            if(string.IsNullOrWhiteSpace(Name) ||
                string.IsNullOrWhiteSpace(Zone) ||
                RapportRarity is null)
            {
                return false;
            }
            
            if(!allMerchantData.ContainsKey(Name)) return false;
            
            var data = allMerchantData[Name];

            return data.Zones.Contains(Zone) &&
                    data.Cards.Any(c => c.Name == Card.Name && c.Rarity == Card.Rarity);
        }
    }
}
