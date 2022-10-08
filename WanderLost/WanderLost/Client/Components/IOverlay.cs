using WanderLost.Shared.Data;

namespace WanderLost.Client.Components
{
    public interface IOverlay
    {
        public Task ShowMerchantGroup(ActiveMerchantGroup merchantGroup, string server, string region);
        public Task ShowMap(string zone);
    }
}
