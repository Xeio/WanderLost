namespace WanderLost.Shared
{
    public class ActiveMerchant
    {
        public string Name { get; init; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public DateTimeOffset NextAppearance { get; private set; }
        public DateTimeOffset AppearanceExpires { get; private set; }

        public void CalculateNextAppearance(Dictionary<string, MerchantData> merchants, string serverTimeZone)
        {
            const int expiresAfter = 25;

            var serverOffset = TimeZoneInfo.FindSystemTimeZoneById(serverTimeZone).BaseUtcOffset;

            var nextAppearanceTime = merchants[Name].AppearanceTimes
                .Select(apperance => DateTimeOffset.UtcNow.ToOffset(serverOffset).Date + apperance)
                .Where(time => time >= DateTimeOffset.UtcNow.AddMinutes(-expiresAfter))
                .FirstOrDefault();
            
            if(nextAppearanceTime == default)
            {
                //Next apperance is the following day
                nextAppearanceTime = merchants[Name].AppearanceTimes
                    .Select(apperance => DateTimeOffset.UtcNow.ToOffset(serverOffset).Date.AddDays(1) + apperance)
                    .Where(time => time >= DateTimeOffset.UtcNow.AddMinutes(-expiresAfter))
                    .FirstOrDefault();
            }

            NextAppearance = nextAppearanceTime;
            AppearanceExpires = nextAppearanceTime.AddMinutes(expiresAfter);
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
