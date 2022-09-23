using WanderLost.Shared.Data;

namespace WanderLost.Client.Components
{
  public interface IOverlay
  {
    public void ShowMerchantGroup(ActiveMerchantGroup MerchantGroup, Dictionary<Guid, VoteType> Votes, string Server, string Region);
    public void ShowMap(string Zone);
  }
}
