namespace WanderLost.Shared
{
    public class ActiveMerchant
    {
        public string Name { get; init; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public DateTimeOffset NextAppearance { get; private set; }
        public DateTimeOffset AppearanceExpires { get; private set; }

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
            Zone = String.Empty;
        }

        public void CopyInstance(ActiveMerchant other)
        {
            Zone = other.Zone;
        }

        public bool IsValidForSubmission()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                    !string.IsNullOrWhiteSpace(Zone);
        }
    }
}
