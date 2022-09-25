namespace WanderLost.Shared.Data
{
    [MessagePack.MessagePackObject]
    public class WeiStats
    {
        [MessagePack.Key(0)]
        public List<(string Server, int WeiCount)> ServerWeiCounts { get; set; } = new();

        [MessagePack.Key(1)]
        public List<(string Server, DateTimeOffset AppearanceTime)> RecentWeis { get; set; } = new();
    }
}