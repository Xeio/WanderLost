namespace WanderLost.Shared.Data;

public class ServerRegion
{
    public string Name { get; init; } = string.Empty;
    /// <summary>
    /// Time Zone server lives in. This should be the <see href="https://www.iana.org/time-zones">IANA name</see>
    /// to be compatible in the Blazor WASM environment.
    /// </summary>
    public string TimeZone { get; init; } = string.Empty;
    public List<string> Servers { get; init; } = new();
}
