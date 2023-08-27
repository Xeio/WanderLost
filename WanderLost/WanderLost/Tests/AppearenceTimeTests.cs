using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WanderLost.Server.Controllers;
using WanderLost.Shared;
using WanderLost.Shared.Data;

namespace Tests
{
    [TestClass]
    public class AppearenceTimeTests
    {
        public static IEnumerable<object[]> KnownMerchants => GetMerchantsFromDatabase();

        public Dictionary<string, MerchantData> MerchantData { get; set; } = new();
        public Dictionary<string, ServerRegion> ServerRegions { get; set; } = new();

        public static IEnumerable<object[]> GetMerchantsFromDatabase()
        {
            var builder = new DbContextOptionsBuilder<MerchantsDbContext>();
            builder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Database=wanderlost-dev");

            using var context = new MerchantsDbContext(builder.Options, Options.Create(new OperationalStoreOptions()));

            return context.ActiveMerchants
                .Where(m => m.ActiveMerchantGroup.NextAppearance > DateTimeOffset.Parse("2023-08-17")) //Time since the merchant update
                .Where(m => m.Votes > 1 && !m.Hidden)
                .Select(m => new object[] { m.ActiveMerchantGroup })
                .ToList();
        }

        [TestInitialize]
        public void Initialize()
        {
            var merchantsFile = File.ReadAllText(@"..\..\..\..\Client\wwwroot\data\merchants.json");
            MerchantData = JsonSerializer.Deserialize<Dictionary<string, MerchantData>>(merchantsFile, Utils.JsonOptions) ?? new();

            var serversFile = File.ReadAllText(@"..\..\..\..\Client\wwwroot\data\servers.json");
            ServerRegions = JsonSerializer.Deserialize<Dictionary<string, ServerRegion>>(serversFile, Utils.JsonOptions) ?? new();
        }

        [DataTestMethod]
        [DynamicData(nameof(KnownMerchants))]
        public void TestMethod(ActiveMerchantGroup group)
        {
            var timeZone = FindServerTimeZone(group.Server);
            if (string.IsNullOrWhiteSpace(timeZone))
            {
                //Couldn't find the time zone for the data to validate against
                return;
            }

            if(!MerchantData.TryGetValue(group.MerchantName, out var merchantData))
            {
                Assert.Fail("Merchant not available in data");
                return;
            }

            group.MerchantData = merchantData;

            var originalAppearance = group.NextAppearance;
            var originalExpires = group.AppearanceExpires;

            group.CalculateNextAppearance(timeZone, originalAppearance.AddMinutes(-1));

            Assert.AreEqual(originalExpires, group.AppearanceExpires, $"Calculated AppearanceExpires invalid for ${BuildMerchantString(group)}");
            Assert.AreEqual(originalAppearance, group.NextAppearance, $"Calculated NextAppearance invalid for ${BuildMerchantString(group)}");

            group.CalculateNextAppearance(timeZone, originalAppearance.AddMinutes(5));

            Assert.AreEqual(originalExpires, group.AppearanceExpires, $"NextAppearance should still be the same during merchant's active window for ${BuildMerchantString(group)}");

            group.CalculateNextAppearance(timeZone, originalAppearance.AddHours(5).AddMinutes(31));

            Assert.IsTrue(group.NextAppearance > originalAppearance, $"After merchant expires should calculate future appearance for ${BuildMerchantString(group)}");
        }

        public string FindServerTimeZone(string server)
        {
            var region = ServerRegions.FirstOrDefault(r => r.Value.Servers.Contains(server));
            return region.Value.TimeZone;
        }

        private static string BuildMerchantString(ActiveMerchantGroup group)
        {
            return $"Merchant: {group.MerchantName}, Group ID: {group.Id}";
        }
    }
}