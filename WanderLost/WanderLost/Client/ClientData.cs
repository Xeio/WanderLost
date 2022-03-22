using WanderLost.Shared.Data;

namespace WanderLost.Client
{
    public class ClientData
    {
        public string Region { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public List<MerchantData>? NotifyingMerchants { get; set; } //null == unset
    }
}
