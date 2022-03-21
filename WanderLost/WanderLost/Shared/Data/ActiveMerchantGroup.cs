

using System.Text.Json.Serialization;

namespace WanderLost.Shared
{
    public class ActiveMerchantGroup
    {
        [JsonIgnore]
        public MerchantData MerchantData { get; init; } = new();

        private string _merchantName = string.Empty;
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

        public void UpdateOrAddMerchant(ActiveMerchant merchant)
        {
            if (ActiveMerchants.FirstOrDefault(x => x.IsEqualTo(merchant)) is ActiveMerchant existing)
            {
                existing.Votes++;
            }
            else
            {
                ActiveMerchants.Add(merchant);
            }
        }

        public void ClearInstances()
        {
            ActiveMerchants.Clear();
        }

        public void ReplaceInstances(List<ActiveMerchant> activeMerchants)
        {
            ActiveMerchants.Clear();
            ActiveMerchants.AddRange(activeMerchants);
        }
    }
}
