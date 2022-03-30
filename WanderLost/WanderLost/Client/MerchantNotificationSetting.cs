using WanderLost.Shared.Data;

namespace WanderLost.Client
{
    public class MerchantNotificationSetting
    {
        public bool Enabled { get; set; }
        public HashSet<string> Cards { get; set; } = new();
    }
}
