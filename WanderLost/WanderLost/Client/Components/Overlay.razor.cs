using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WanderLost.Shared.Data;
using Microsoft.AspNetCore.Components.Routing;

namespace WanderLost.Client.Components;

public partial class Overlay : IOverlay, IDisposable
{
  [Inject]
  private NavigationManager NavigationManager { get; set; }

  [Inject]
  private IJSRuntime JSRuntime { get; set; }

  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  public ActiveMerchantGroup? MerchantGroup;
  public string? Server;
  public string? Zone;
  public string? Region;


  public bool isActive
  {
    get
    {
      return ((this.MerchantGroup is not null) && (this.Server is not null)) || (this.Zone is not null);
    }
  }

  public void ShowMap(string Zone)
  {
    this.MerchantGroup = null;
    this.Server = null;
    this.Region = null;
    this.Zone = Zone;
    JSRuntime.InvokeVoidAsync("HideBodyScroll");
  }

  public void ShowMerchantGroup(ActiveMerchantGroup MerchantGroup, string Server, string Region)
  {
    this.MerchantGroup = MerchantGroup;
    this.Server = Server;
    this.Region= Region;
    this.Zone = null;
    JSRuntime.InvokeVoidAsync("HideBodyScroll");
  }

  public void Close()
  {
    this.MerchantGroup = null;
    this.Server = null;
    this.Region = null;
    this.Zone = null;
    JSRuntime.InvokeVoidAsync("ShowBodyScroll");
  }

  protected override void OnInitialized()
  {
    NavigationManager.LocationChanged += HandleLocationChanged;
  }

  private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
  {
    Close();
  }

  public void Dispose()
  {
    NavigationManager.LocationChanged -= HandleLocationChanged;
  }
}

