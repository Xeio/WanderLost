using WanderLost.Shared.Data;

namespace WanderLost.Client.Services;

public class ActiveDataController(ClientStaticDataController _staticData)
{
    public List<ActiveMerchantGroup> MerchantGroups { get; private set; } = [];
    public Dictionary<Guid, ActiveMerchant> MerchantDictionary { get; private set; } = [];
    public Dictionary<Guid, VoteType> Votes { get; private set; } = [];

    public async Task Init()
    {
        await _staticData.Init();

        if (MerchantGroups.Count == 0)
        {
            MerchantGroups.AddRange(_staticData.Merchants.Values.Select(m => new ActiveMerchantGroup() { MerchantData = m }));
        }
    }
}
