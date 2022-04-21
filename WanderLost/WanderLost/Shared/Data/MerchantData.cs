namespace WanderLost.Shared.Data
{
    public class MerchantData
    {
        public string Name { get; init; } = string.Empty;
        public string Region { get; init; } = string.Empty;
        public List<TimeSpan> AppearanceTimes { get; init; } = new();
        public List<string> Zones { get; init; } = new();
        public List<Item> Cards { get; init; } = new();
        public List<Item> Rapports { get; init; } = new();
    }
}
