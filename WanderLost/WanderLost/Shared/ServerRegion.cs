namespace WanderLost.Shared
{
    public class ServerRegion
    {
        public string Name { get; init; } = string.Empty;
        public string TimeZone { get; init; } = string.Empty;
        public List<string> Servers { get; init; } = new();
    }
}
