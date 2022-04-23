using WanderLost.Shared.Data;

namespace WanderLost.Client.Services
{
    public class ActiveDataController
    {
        public int DownvoteThresholdUntilRemoved { get; init; } = -10;

        public List<ActiveMerchantGroup> MerchantGroups { get; private set; } = new();
        public Dictionary<Guid, ActiveMerchant> MerchantDictionary { get; private set; } = new();
        public Dictionary<Guid, VoteType> Votes { get; private set; } = new();

        private readonly ClientStaticDataController StaticData;

        public ActiveDataController(ClientStaticDataController staticData)
        {
            StaticData = staticData;
        }

        public async Task Init()
        {
            await StaticData.Init();

            if (MerchantGroups.Count == 0)
            {
                MerchantGroups.AddRange(StaticData.Merchants.Values.Select(m => new ActiveMerchantGroup() { MerchantData = m }));
            }
        }
    }
}
