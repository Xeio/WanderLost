using WanderLost.Shared.Data;

namespace WanderLost.Client
{
    public static class ActiveMerchantGroupExtensions
    {

        public static bool HasDifferentMerchantsTo(this ActiveMerchantGroup baseGroup, ActiveMerchantGroup compareGroup)
        {
            //Do groups have different counts or merchants with different names?
            return baseGroup.ActiveMerchants.Count != compareGroup.ActiveMerchants.Count ||
                    (!compareGroup.ActiveMerchants.All(searcher => baseGroup.ActiveMerchants.Any(finder => finder.Name == searcher.Name)) &&
                        !baseGroup.ActiveMerchants.All(searcher => compareGroup.ActiveMerchants.Any(finder => finder.Name == searcher.Name)));
        }
    }
}
