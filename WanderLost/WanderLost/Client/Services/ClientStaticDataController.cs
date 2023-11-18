using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using WanderLost.Shared;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Services;

public class ClientStaticDataController
{
    public bool Initialized { get; private set; }
    public Dictionary<string, ServerRegion> ServerRegions { get; private set; } = [];
    public Dictionary<string, MerchantData> Merchants { get; private set; } = [];
    public Dictionary<string, string> Tooltips { get; private set; } = [];

    private readonly NavigationManager _navigationManager;
    private readonly HttpClient _httpClient;

    public ClientStaticDataController(NavigationManager navigationManager, HttpClient httpClient)
    {
        _navigationManager = navigationManager;
        _httpClient = httpClient;
    }

    public async Task Init()
    {
        if (Initialized) return;

        Merchants = await _httpClient.GetFromJsonAsync<Dictionary<string, MerchantData>>(_navigationManager.ToAbsoluteUri("/data/merchants.json"), Utils.JsonOptions) ?? [];
        ServerRegions = await _httpClient.GetFromJsonAsync<Dictionary<string, ServerRegion>>(_navigationManager.ToAbsoluteUri("/data/servers.json"), Utils.JsonOptions) ?? [];
        Tooltips = await _httpClient.GetFromJsonAsync<Dictionary<string, string>>(_navigationManager.ToAbsoluteUri("/data/tooltips.json"), Utils.JsonOptions) ?? [];

        Initialized = true;
    }
}
