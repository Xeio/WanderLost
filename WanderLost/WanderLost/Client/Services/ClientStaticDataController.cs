using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using WanderLost.Shared;
using WanderLost.Shared.Data;

namespace WanderLost.Client.Services
{
    public class ClientStaticDataController
    {
        public bool Initialized { get; private set; }
        public Dictionary<string, ServerRegion> ServerRegions { get; private set; } = new();
        public Dictionary<string, MerchantData> Merchants { get; private set; } = new();
        public Dictionary<string, string> Tooltips { get; private set; } = new();

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

            Merchants = await _httpClient.GetFromJsonAsync<Dictionary<string, MerchantData>>(_navigationManager.ToAbsoluteUri("/data/merchants.json"), Utils.JsonOptions) ?? new();
            Utils.GenerateDebugTestMerchant(Merchants);

            ServerRegions = await _httpClient.GetFromJsonAsync<Dictionary<string, ServerRegion>>(_navigationManager.ToAbsoluteUri("/data/servers.json"), Utils.JsonOptions) ?? new();
            Tooltips = await _httpClient.GetFromJsonAsync<Dictionary<string, string>>(_navigationManager.ToAbsoluteUri("/data/tooltips.json"), Utils.JsonOptions) ?? new();

            Initialized = true;
        }
    }
}
