namespace WanderLost.Shared
{
    public class ActiveMerchant
    {
        public string Name { get; init; } = string.Empty;
        public DateTime NextAppearance { get; set; }
        public DateTime AppearanceExpires { get; private set; }

        public void CalculateNextAppearance(Dictionary<string, MerchantData> merchants, string timeZone)
        {

            const int expiresAfter = 25;
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);

            var nextAppearanceTime = merchants[Name].AppearanceTimes
                .Select(apperance => DateTime.UtcNow.Date - timeZoneInfo.BaseUtcOffset + apperance)
                .Where(time => time >= DateTime.UtcNow.AddMinutes(-expiresAfter))
                .FirstOrDefault();
            
            if(nextAppearanceTime == default)
            {
                //Next apperance is the following day
                nextAppearanceTime = merchants[Name].AppearanceTimes
                    .Select(apperance => DateTime.UtcNow.Date.AddDays(1) - timeZoneInfo.BaseUtcOffset + apperance)
                    .Where(time => time >= DateTime.UtcNow.AddMinutes(-expiresAfter))
                    .FirstOrDefault();
            }

            NextAppearance = nextAppearanceTime;
            AppearanceExpires = nextAppearanceTime.AddMinutes(expiresAfter);
        }
    }
}
