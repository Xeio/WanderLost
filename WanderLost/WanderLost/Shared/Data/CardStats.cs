namespace WanderLost.Shared.Data
{
    [MessagePack.MessagePackObject]
    public class CardStats
    {
        [MessagePack.Key(0)]
        public List<(string Server, int Count)> ServerCardCounts { get; set; } = [];

        [MessagePack.Key(1)]
        public List<(string Server, DateTimeOffset AppearanceTime)> RecentAppearances { get; set; } = [];
    }
}