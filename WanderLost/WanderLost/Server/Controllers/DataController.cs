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

        public Dictionary<string, MerchantData> MerchantData
        {
            get
            {
                return _memoryCache.GetOrCreate(nameof(MerchantData), BuildMerchantData);
            }
        }

        private Dictionary<string, MerchantData> BuildMerchantData(ICacheEntry entry)
        {
            var serversFile = _webHostEnvironment.WebRootFileProvider.GetFileInfo("data/merchants.json");
            var json = File.ReadAllText(serversFile.PhysicalPath);
            return JsonSerializer.Deserialize<Dictionary<string, MerchantData>>(json, Utils.JsonOptions) ?? new Dictionary<string, MerchantData>();
        }

        public Dictionary<string, ServerRegion> ServerRegions
        {
            get
            {
                return _memoryCache.GetOrCreate(nameof(MerchantData), BuildServerData);
            }
        }

        private Dictionary<string, ServerRegion> BuildServerData(ICacheEntry entry)
        {
            var serversFile = _webHostEnvironment.WebRootFileProvider.GetFileInfo("data/servers.json");
            var json = File.ReadAllText(serversFile.PhysicalPath);
            return JsonSerializer.Deserialize<Dictionary<string, ServerRegion>>(json, Utils.JsonOptions) ?? new Dictionary<string, ServerRegion>();
        }
    }
}
