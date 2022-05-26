using MessagePack;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using WanderLost.Shared.Data;

namespace WanderLost.Shared;

public static class Utils
{
    public static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters =
        {
            new JsonStringEnumConverter()
        },
    };

    /// <summary>
    /// Build message pack options shared by both client and server
    /// </summary>
    public static void BuildMessagePackOptions(MessagePackHubProtocolOptions messagePackOptions)
    {
        messagePackOptions.SerializerOptions = MessagePackSerializerOptions.Standard
            .WithResolver(MessagePack.Resolvers.CompositeResolver.Create(
                    MessagePack.Resolvers.NativeGuidResolver.Instance,
                    MessagePack.Resolvers.StandardResolver.Instance
                ))
            .WithSecurity(MessagePackSecurity.UntrustedData)
            .WithCompression(MessagePackCompression.Lz4Block);
    }

    [Conditional("DEBUG")]
    public static void GenerateDebugTestMerchant(Dictionary<string, MerchantData> merchants)
    {
        List<TimeSpan> times = new();
        for (var time = TimeSpan.Zero; time < TimeSpan.FromHours(24); time = time.Add(TimeSpan.FromMinutes(25)))
        {
            times.Add(time);
        }
        merchants.Add("TESTONLY", new MerchantData()
        {
            Name = "TESTONLY",
            Region = "TestRegion",
            Zones = { "Twilight Zone", "Cardassia Prime", "Medina Station" },
            Cards = 
            {
                new Item() { Name = "Jack O'Neill", Rarity = Rarity.Relic },
                new Item() { Name = "Jaffa", Rarity = Rarity.Normal },
            },
            Rapports =
            {
                new Item() { Name = "Lightsaber", Rarity = Rarity.Legendary },
                new Item() { Name = "Half-Eaten Cookie", Rarity = Rarity.Normal },
            },
            AppearanceTimes = times,
        });
    }
}
