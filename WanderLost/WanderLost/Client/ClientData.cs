using WanderLost.Shared.Data;

namespace WanderLost.Client
{
    public class ClientData
    {
        public string Region { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;

        //Should probably group the below stuff into its own subclass...
        public List<MerchantData>? NotifyingMerchants { get; set; } //null == unset
        public bool NotifyMerchantAppearance { get; set; } = true;
        public bool NotificationsEnabled { get; set; } = false;
    }
}
