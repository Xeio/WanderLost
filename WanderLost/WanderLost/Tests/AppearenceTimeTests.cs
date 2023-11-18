using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WanderLost.Server.Controllers;
using WanderLost.Shared;
using WanderLost.Shared.Data;

namespace WanderLost.Tests;

[TestClass]
public class AppearenceTimeTests
{
    public static IEnumerable<object[]> KnownMerchants => GetMerchantsFromDatabase();

    public Dictionary<string, MerchantData> MerchantData { get; set; } = [];
    public Dictionary<string, ServerRegion> ServerRegions { get; set; } = [];

    public static IEnumerable<object[]> GetMerchantsFromDatabase()
    {
        var builder = new DbContextOptionsBuilder<MerchantsDbContext>();
        builder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Database=wanderlost-dev");

        using var context = new MerchantsDbContext(builder.Options, Options.Create(new OperationalStoreOptions()));

        return context.ActiveMerchants
            //Check against range where merchants were freely enterable
            .Where(m => m.ActiveMerchantGroup.NextAppearance > DateTimeOffset.Parse("2023-08-17") &&
                        m.ActiveMerchantGroup.NextAppearance < DateTimeOffset.Parse("2023-08-26"))
            .Where(m => m.Votes > 1 && !m.Hidden)
            .Select(m => new object[] { m.ActiveMerchantGroup })
            .ToList();
    }

    [TestInitialize]
    public void Initialize()
    {
        var merchantsFile = File.ReadAllText(@"..\..\..\..\Client\wwwroot\data\merchants.json");
        MerchantData = JsonSerializer.Deserialize<Dictionary<string, MerchantData>>(merchantsFile, Utils.JsonOptions) ?? [];

        var serversFile = File.ReadAllText(@"..\..\..\..\Client\wwwroot\data\servers.json");
        ServerRegions = JsonSerializer.Deserialize<Dictionary<string, ServerRegion>>(serversFile, Utils.JsonOptions) ?? [];
    }

    [DataTestMethod]
    [DynamicData(nameof(KnownMerchants))]
    public void TestMerchantAppearanceTimeCalculation(ActiveMerchantGroup group)
    {
        var serverRegion = FindServerRegion(group.Server);
        if (string.IsNullOrWhiteSpace(serverRegion.TimeZone))
        {
            //Couldn't find the time zone for the data to validate against
            return;
        }

        if(serverRegion.Name == "South America" ||
            group.Server == "Ealyn" ||
            group.Server == "Nia")
        {
            return;
        }

        if (!MerchantData.TryGetValue(group.MerchantName, out var merchantData))
        {
            Assert.Fail("Merchant not available in data");
            return;
        }

        group.MerchantData = merchantData;

        var originalAppearance = group.NextAppearance;
        var originalExpires = group.AppearanceExpires;

        group.CalculateNextAppearance(serverRegion.TimeZone, originalAppearance.AddMinutes(-1));

        Assert.AreEqual(originalExpires, group.AppearanceExpires, $"Calculated AppearanceExpires invalid for {BuildMerchantString(group)}");
        Assert.AreEqual(originalAppearance, group.NextAppearance, $"Calculated NextAppearance invalid for {BuildMerchantString(group)}");

        group.CalculateNextAppearance(serverRegion.TimeZone, originalAppearance.AddMinutes(5));

        Assert.AreEqual(originalExpires, group.AppearanceExpires, $"NextAppearance should still be the same during merchant's active window for {BuildMerchantString(group)}");

        group.CalculateNextAppearance(serverRegion.TimeZone, originalAppearance.AddHours(5).AddMinutes(31));

        Assert.IsTrue(group.NextAppearance > originalAppearance, $"After merchant expires should calculate future appearance for {BuildMerchantString(group)}");
    }

    public ServerRegion FindServerRegion(string server)
    {
        var region = ServerRegions.FirstOrDefault(r => r.Value.Servers.Contains(server));
        return region.Value;
    }

    private static string BuildMerchantString(ActiveMerchantGroup group)
    {
        return $"Merchant: {group.MerchantName}, Group ID: {group.Id}, Server: {group.Server}";
    }
}