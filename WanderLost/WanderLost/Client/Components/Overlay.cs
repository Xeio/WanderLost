using Microsoft.AspNetCore.Components;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Components
{
  public interface IOverlay
  {
    public void ShowMerchantGroup(ActiveMerchantGroup MerchantGroup, string Server, string Region);
    public void ShowMap(string Zone);
  }
}
