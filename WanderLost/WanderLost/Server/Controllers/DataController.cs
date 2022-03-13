using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using WanderLost.Shared;

namespace WanderLost.Server.Controllers
{
    public class DataController
    {
        private IWebHostEnvironment _webHostEnvironment;

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
            return JsonSerializer.Deserialize<Dictionary<string, MerchantData>>(json, Utils.JsonOptions) ?? new Dictionary<string, MerchantData>();
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

        public async Task<List<ActiveMerchant>> GetActiveMerchants(string server)
        {
            return await _memoryCache.GetOrCreateAsync(server, async (cacheEntry) =>
                {
                    var activeMerchants = await BuildActiveMerchants(cacheEntry, server);
                    //Force the cache to expire once any merchant apperance expires
                    cacheEntry.AbsoluteExpiration = activeMerchants.Min(m => m.AppearanceExpires);
                    return activeMerchants;
                }
            );
        }

        private async Task<List<ActiveMerchant>> BuildActiveMerchants(ICacheEntry entry, string server)
        {
            var merchants = await GetMerchantData();
            var regions = await GetServerRegions();
            
            var currentRegion = regions.FirstOrDefault(r => r.Value.Servers.Contains(server));

            if (currentRegion.Value == null) throw new ArgumentException("Invalid Server");

            var activeMerchants = merchants.Select(m => new ActiveMerchant() { Name = m.Value.Name }).ToList();
            foreach(var activeMerchant in activeMerchants)
            {
                activeMerchant.CalculateNextAppearance(merchants, currentRegion.Value.UtcOffset);
            }

            return activeMerchants;
        }
    }
}
