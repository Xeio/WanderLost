namespace WanderLost.Shared.Interfaces
{
    public interface IMerchantHubShared
    {
        Task UpdateMerchant(string server, ActiveMerchant merchant);
        Task UpdateMerchantGroup(string server, ActiveMerchantGroup merchantGroup);

    }
}
