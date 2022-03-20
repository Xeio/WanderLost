using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WanderLost.Shared
{
    public class ActiveMerchantGroup
    {
        public string Id { get; init; } = "";
        public List<ActiveMerchant> Merchants { get; init; } = new List<ActiveMerchant>();
        public ActiveMerchant MostVotedMerchant { get => Merchants.MaxBy(x => x.Votes) ?? throw new NullReferenceException(); }

        public ActiveMerchantGroup() { }
        public ActiveMerchantGroup(ActiveMerchant firstMerchant)
        {
            Id = firstMerchant.Name;
            UpdateOrAddMerchant(firstMerchant);
        }

        public void UpdateOrAddMerchant(ActiveMerchant merchant)
        {
            if (Merchants.FirstOrDefault(x => x.IsEqualTo(merchant)) is ActiveMerchant existing)
            {
                existing.Votes++;
            }
            else
            {
                Merchants.Add(merchant);
            }
        }

        public void ClearPlaceholderMerchant()
        {
            if (Merchants.FirstOrDefault(x => x.Zone == "") is ActiveMerchant existing)
            {
                Merchants.Remove(existing);
            }
        }

        public void ClearInstances()
        {
            Merchants.ForEach(x => x.ClearInstance());
        }

        public void CopyInstance(ActiveMerchantGroup amg)
        {
            Merchants.Clear();
            Merchants.AddRange(amg.Merchants);
        }

        public void CalculateNextAppearances(Dictionary<string, MerchantData> merchants, TimeSpan serverUtcOffset)
        {
            Merchants.ForEach(x => x.CalculateNextAppearance(merchants, serverUtcOffset));
        }
    }
}
