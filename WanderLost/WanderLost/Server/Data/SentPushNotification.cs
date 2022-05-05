using WanderLost.Shared.Data;

namespace WanderLost.Server.Data
{
    public class SentPushNotification
    {
        public Guid MerchantId { get; set; }
        public ActiveMerchant Merchant { get; set; } = new();
        public int SubscriptionId { get; set; }
    }
}
