namespace WanderLost.Shared
{
    public class MerchantData
    {
        public string Name { get; init; } = string.Empty;
        public string Region { get; init; } = string.Empty;
        public List<TimeSpan> AppearanceTimes { get; init; } = new();
        public List<string> Zones { get; init; } = new();
        public List<string> Cards { get; init; } = new();
    }
}
