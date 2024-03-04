using System.Text.Json;
using WanderLost.Shared;
using WanderLost.Shared.Data;

namespace WanderLost.Tests;

[TestClass]
public class ServerMergeTests
{
    public static IEnumerable<object[]> AllServerNames { get; private set; } = [];
    public static HashSet<string> ServersInJson { get; private set; } = [];
    public static IEnumerable<object[]> MergeTargets { get; private set; } = [];
    public static IEnumerable<object[]> MergeSources { get; private set; } = [];

    static ServerMergeTests()
    {
        var serversFile = File.ReadAllText(@"..\..\..\..\Client\wwwroot\data\servers.json");
        var serverRegions = JsonSerializer.Deserialize<Dictionary<string, ServerRegion>>(serversFile, Utils.JsonOptions) ?? [];

        ServersInJson = serverRegions.SelectMany(r => r.Value.Servers).ToHashSet();
        AllServerNames = ServersInJson.Select(s => new object[] { s });
        MergeSources = Utils.ServerMerges.Keys.Distinct().Select(s => new object[] { s });
        MergeTargets = Utils.ServerMerges.Values.Distinct().Select(s => new object[] { s });
    }

    [DataTestMethod]

    [DynamicData(nameof(AllServerNames))]
    public void MergedServersDontExistInData(string server)
    {
        Assert.IsFalse(Utils.HasMergedServer(server, out _), $"'{server}' is a merged server but still exists in json");
    }

    [DataTestMethod]
    [DynamicData(nameof(MergeSources))]
    public void ServersArentRecursivelyMerged(string server)
    {
        if(Utils.ServerMerges.TryGetValue(server, out var mergedServer) && Utils.HasMergedServer(server, out var trueMergeTarget))
        {
            Assert.AreEqual(mergedServer, trueMergeTarget, $"'{server}' is recursively merged to '{trueMergeTarget}'");
        }
    }

    [DataTestMethod]
    [DynamicData(nameof(MergeTargets))]
    public void AllServerMergeTargetsExist(string mergeTargetServer)
    {
        Assert.IsTrue(ServersInJson.Contains(mergeTargetServer), $"'{mergeTargetServer}' is a marge target, but doesn't exsit in json");
    }

    [DataTestMethod]
    [DynamicData(nameof(MergeSources))]
    public void MergeSourcesShouldNotExist(string sourceMergeServer)
    {
        Assert.IsFalse(ServersInJson.Contains(sourceMergeServer), $"'{sourceMergeServer}' is a source merge server, but still exists in json");
    }
}
