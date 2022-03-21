using WanderLost.Shared.Data;

namespace WanderLost.Shared.Interfaces
{
    public interface IMerchantHubClient : IMerchantHubShared
    {
        Task UpdateMerchantGroup(string server, ActiveMerchantGroup merchantGroup);
    }
}
