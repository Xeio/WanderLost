namespace WanderLost.Client;

public class MerchantNotificationSetting
{
    public bool NotifySpawn { get; set; }
    public HashSet<string> Cards { get; set; } = [];
    public HashSet<string> Rapports { get; set; } = [];
    public HashSet<string> MiscItems { get; set; } = [];
    public HashSet<string> Tradeskills { get; set; } = [];
}
