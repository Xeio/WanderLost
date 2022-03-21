namespace WanderLost.Shared.Data
{
    public class ServerRegion
    {
        public string Name { get; init; } = string.Empty;
        public TimeSpan UtcOffset { get; init; }
        public List<string> Servers { get; init; } = new();
    }
}
