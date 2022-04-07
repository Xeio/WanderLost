using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WanderLost.Shared.Data
{
    public class ActiveMerchantGroup
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        [MaxLength(20)]
        public string Server { get; set; } = string.Empty;

        [NotMapped]
        [JsonIgnore]
        public MerchantData MerchantData { get; init; } = new();

        private string _merchantName = string.Empty;
        [MaxLength(20)]
        public string MerchantName
        {
            get
            {
                //If merchant group came across network, it won't have data but the name will be serialized as its own property
                if (!string.IsNullOrWhiteSpace(MerchantData.Name))
                {
                    return MerchantData.Name;
                }
                else
                {
                    return _merchantName;
                }
            }
            init
            {
                _merchantName = value;
            }
        }

        public List<ActiveMerchant> ActiveMerchants { get; init; } = new List<ActiveMerchant>();

        [JsonIgnore]
        public DateTimeOffset NextAppearance { get; private set; }

        [JsonIgnore]
        public DateTimeOffset AppearanceExpires { get; private set; }

        [NotMapped]
        public bool IsActive => DateTimeOffset.UtcNow > NextAppearance && DateTimeOffset.UtcNow < AppearanceExpires;

        public void CalculateNextAppearance(TimeSpan serverUtcOffset)
        {
            var expiresAfter = TimeSpan.FromMinutes(25);

            var nextAppearanceTime = MerchantData.AppearanceTimes
                .Select(apperance => new DateTimeOffset(DateTimeOffset.UtcNow.ToOffset(serverUtcOffset).Date, serverUtcOffset) + apperance)
                .Where(time => time >= DateTimeOffset.UtcNow - expiresAfter)
                .FirstOrDefault();

            if (nextAppearanceTime == default)
            {
                //Next apperance is the following day
                nextAppearanceTime = MerchantData.AppearanceTimes
                    .Select(apperance => new DateTimeOffset(DateTimeOffset.UtcNow.ToOffset(serverUtcOffset).Date.AddDays(1), serverUtcOffset) + apperance)
                    .Where(time => time >= DateTimeOffset.UtcNow - expiresAfter)
                    .FirstOrDefault();
            }

            NextAppearance = nextAppearanceTime;
            AppearanceExpires = nextAppearanceTime + expiresAfter;
        }

        public void ClearInstances()
        {
            ActiveMerchants.Clear();
        }

        public void ReplaceInstances(List<ActiveMerchant> activeMerchants)
        {
            ActiveMerchants.Clear();
            ActiveMerchants.AddRange(activeMerchants);
            ActiveMerchants.Sort((a, b) => b.Votes - a.Votes);
        }
    }
}
