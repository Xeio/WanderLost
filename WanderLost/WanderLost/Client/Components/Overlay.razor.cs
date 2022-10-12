using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WanderLost.Shared.Data;
using Microsoft.AspNetCore.Components.Routing;

namespace WanderLost.Client.Components;

public partial class Overlay : IOverlay, IDisposable
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public ActiveMerchantGroup? MerchantGroup;
    public string? Server;
    public string? Zone;
    public string? Region;

    public bool IsActive
    {
        get
        {
            return (MerchantGroup is not null && Server is not null) || Zone is not null;
        }
    }

    public async Task ShowMap(string zone)
    {
        MerchantGroup = null;
        Server = null;
        Region = null;
        Zone = zone;
        await InvokeAsync(StateHasChanged);
        await JSRuntime.InvokeVoidAsync("HideBodyScroll");
    }

    public async Task ShowMerchantGroup(ActiveMerchantGroup merchantGroup, string server, string region)
    {
        MerchantGroup = merchantGroup;
        Server = server;
        Region = region;
        Zone = null;
        await InvokeAsync(StateHasChanged);
        await JSRuntime.InvokeVoidAsync("HideBodyScroll");
    }

    public async Task Close()
    {
        MerchantGroup = null;
        Server = null;
        Region = null;
        Zone = null;
        await InvokeAsync(StateHasChanged);
        await JSRuntime.InvokeVoidAsync("ShowBodyScroll");
    }

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    private async void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        await Close();
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
    }
}

