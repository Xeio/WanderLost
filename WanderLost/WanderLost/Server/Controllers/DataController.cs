using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using WanderLost.Shared;
using WanderLost.Shared.Data;

namespace WanderLost.Server.Controllers;

public class DataController
{
    private static readonly SemaphoreSlim _semaphore = new(1);

    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<DataController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public DataController(IWebHostEnvironment webHostEnvironment, IMemoryCache memoryCache, ILogger<DataController> logger, IHttpClientFactory httpClientFactory)
    {
        _webHostEnvironment = webHostEnvironment;
        _memoryCache = memoryCache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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

    public async Task<bool> IsServerOnline(string server)
    {
        if (!_memoryCache.TryGetValue(nameof(IsServerOnline), out Dictionary<string, bool>? statuses))
        {
            await _semaphore.WaitAsync();
            try
            {
                statuses = await _memoryCache.GetOrCreateAsync(nameof(IsServerOnline), BuildServerOnlineStates);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        if(statuses is null)
        {
            //Some error happened while retrieving statuses, assume servers are online
            return true;
        }

        if (statuses.TryGetValue(server, out var status))
        {
            return status;
        }

        //Sometimes instead of showing offline, a server just won't be present on the status page
        return false;
    }

    private async Task<Dictionary<string, bool>?> BuildServerOnlineStates(ICacheEntry entry)
    {
        try
        {
            var html = await _httpClientFactory.CreateClient().GetStringAsync("https://www.playlostark.com/en-us/support/server-status");
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var serverStatusnodes = doc.DocumentNode.Descendants().Where(n => n.HasClass("ags-ServerStatus-content-responses-response-server"));
            
            var onlineStates = new Dictionary<string, bool>();
            foreach (var node in serverStatusnodes)
            {
                var nameNode = node.Descendants().FirstOrDefault(n => n.HasClass("ags-ServerStatus-content-responses-response-server-name"));
                var name = nameNode?.InnerText?.Trim();
                var serverInMaintenance = node.Descendants().Any(n => n.HasClass("ags-ServerStatus-content-responses-response-server-status--maintenance"));
                if (!string.IsNullOrWhiteSpace(name))
                {
                    if(Utils.HasMergedServer(name, out var mergedServer))
                    {
                        name = mergedServer;
                    }
                    onlineStates[name] = !serverInMaintenance;
                }
            }

            var allServers = (await GetServerRegions()).SelectMany(r => r.Value.Servers);

            var missingServers = allServers.Except(onlineStates.Keys);
            var onlineCount = onlineStates.Count(s => s.Value);
            var totalCount = allServers.Count();

            if (missingServers.Any())
            {
                _logger.LogInformation("Servers not detected on status page: {missingServers}", string.Join(", ", missingServers));
                //If servers are missing, then game is either in maintainence, or we got an intermittent issue on the status page
                //Allow status checks a bit more often (if truly in maintenence, there will rarely be submission for this to fire anyway)
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            }
            else if (onlineCount < totalCount)
            {
                //At least one server in maintenence mode
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            }
            else 
            {
                //All servers are online
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            }

            _logger.LogInformation("Server status {onlineCount} online of {total}.", onlineCount, totalCount);
            return onlineStates;
        }
        catch(Exception e)
        {
            //If for any reason this fails, we'll just return a null and assume that all servers are online
            _logger.LogError(e, "Failed to retrieve Lost Ark server status.");
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return null;
        }
    }
}
