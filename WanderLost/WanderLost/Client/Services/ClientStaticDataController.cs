using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using WanderLost.Shared;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Services;

public class ClientStaticDataController(NavigationManager _navigationManager, HttpClient _httpClient)
{
    public bool Initialized { get; private set; }
    public Dictionary<string, ServerRegion> ServerRegions { get; private set; } = [];
    public Dictionary<string, MerchantData> Merchants { get; private set; } = [];
    public Dictionary<string, string> Tooltips { get; private set; } = [];

    public async Task Init()
    {
        if (Initialized) return;

        Merchants = await _httpClient.GetFromJsonAsync<Dictionary<string, MerchantData>>(_navigationManager.ToAbsoluteUri("/data/merchants.json"), Utils.JsonOptions) ?? [];
        ServerRegions = await _httpClient.GetFromJsonAsync<Dictionary<string, ServerRegion>>(_navigationManager.ToAbsoluteUri("/data/servers.json"), Utils.JsonOptions) ?? [];
        Tooltips = await _httpClient.GetFromJsonAsync<Dictionary<string, string>>(_navigationManager.ToAbsoluteUri("/data/tooltips.json"), Utils.JsonOptions) ?? [];

        Initialized = true;
    }
}
