﻿using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using WanderLost.Shared;
using WanderLost.Shared.Data;

namespace WanderLost.Server.Controllers
{
    public class DataController
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly IMemoryCache _memoryCache;

        public DataController(IWebHostEnvironment webHostEnvironment, IMemoryCache memoryCache)
        {
            _webHostEnvironment = webHostEnvironment;
            _memoryCache = memoryCache;
        }

        public async Task<Dictionary<string, MerchantData>> GetMerchantData()
        {
            return await _memoryCache.GetOrCreateAsync(nameof(MerchantData), BuildMerchantData);
        }

        private async Task<Dictionary<string, MerchantData>> BuildMerchantData(ICacheEntry entry)
        {
            var serversFile = _webHostEnvironment.WebRootFileProvider.GetFileInfo("data/merchants.json");
            var json = await File.ReadAllTextAsync(serversFile.PhysicalPath);
            var merchantData = JsonSerializer.Deserialize<Dictionary<string, MerchantData>>(json, Utils.JsonOptions) ?? new Dictionary<string, MerchantData>();
            Utils.GenerateDebugTestMerchant(merchantData);
            return merchantData;
        }

        public async Task<Dictionary<string, ServerRegion>> GetServerRegions()
        {
            return await _memoryCache.GetOrCreateAsync(nameof(ServerRegion), BuildServerData);
        }

        private async Task<Dictionary<string, ServerRegion>> BuildServerData(ICacheEntry entry)
        {
            var serversFile = _webHostEnvironment.WebRootFileProvider.GetFileInfo("data/servers.json");
            var json = await File.ReadAllTextAsync(serversFile.PhysicalPath);
            return JsonSerializer.Deserialize<Dictionary<string, ServerRegion>>(json, Utils.JsonOptions) ?? new Dictionary<string, ServerRegion>();
        }

        public async Task<List<ActiveMerchantGroup>> GetActiveMerchantGroups(string server)
        {
            return await _memoryCache.GetOrCreateAsync(server, async (cacheEntry) =>
            {
                var merchantGroups = await BuildActiveMerchantGroups(server);
                //Force the cache to expire once any merchant apperance expires
                cacheEntry.AbsoluteExpiration = merchantGroups.Min(m => m.AppearanceExpires);
                return merchantGroups;
            });
        }

        private async Task<List<ActiveMerchantGroup>> BuildActiveMerchantGroups(string server)
        {
            var merchants = await GetMerchantData();
            var regions = await GetServerRegions();

            var currentRegion = regions.FirstOrDefault(r => r.Value.Servers.Contains(server));

            if (currentRegion.Value == null) throw new ArgumentException("Invalid Server");

            var activeMerchantGroups = merchants.Values.Select(m => new ActiveMerchantGroup() { MerchantData = m }).ToList();
            foreach (var activeMerchant in activeMerchantGroups)
            {
                activeMerchant.CalculateNextAppearance(currentRegion.Value.UtcOffset);
            }

            return activeMerchantGroups;
        }
    }
}
