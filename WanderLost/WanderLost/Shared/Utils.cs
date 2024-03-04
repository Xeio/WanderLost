using MessagePack;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    public static readonly IReadOnlyDictionary<string, string> ServerMerges = new Dictionary<string, string>()
    {
        { "Shadespire", "Arcturus" },
        { "Petrania", "Arcturus" },
        { "Tragon", "Arcturus" },
        { "Stonehearth", "Arcturus" },
        { "Kurzan", "Vairgrys" },
        { "Prideholme", "Vairgrys" },
        { "Yorn", "Vairgrys" },
        { "Feiton", "Vairgrys" },
        { "Sirius", "Elpon" },
        { "Sceptrum", "Elpon" },
        { "Procyon", "Gienah" },
        { "Beatrice", "Arcturus" },
        { "Rethramis", "Arcturus" },
        { "Tortoyk", "Arcturus" },
        { "Moonkeep", "Arcturus" },
        { "Punika", "Arcturus" },
        { "Agaton", "Vairgrys" },
        { "Vern", "Vairgrys" },
        { "Antares", "Elpon" },
        { "Slen", "Elpon" },
        { "Calvasus", "Arcturus" },
        { "Mokoko", "Gienah" },
        { "Lazenith", "Gienah" },
        { "Ezrebet", "Ratik" },
        { "Trixion", "Ratik" },
        { "Armen", "Elpon" },
        { "Evergrace", "Arcturus" },
        { "Aldebaran", "Luterra" },
        { "Zosma", "Balthorr" },
        { "Vykas", "Nineveh" },
        { "Danube", "Inanna" },
        { "Elzowin", "Inanna" },
        { "Kharmine", "Luterra" },
        { "Adrinne", "Nineveh" },
        { "Sasha", "Balthorr" },
        { "Bergstrom", "Thaemine" },
        { "Enviska", "Brelshaza" },
        { "Shandi", "Thaemine" },
        { "Akkan", "Brelshaza" },
        { "Kazeros", "Vairgrys" },
        { "Blackfang", "Vairgrys" },
        { "Neria", "Ortuus" },
        { "Wei", "Elpon" },
        { "Kadan", "Elpon" },
        { "Asta", "Ratik" },
        { "Zinnervale", "Ratik" },
        { "Nia", "Arcturus" },
        { "Ealyn", "Arcturus" },
        { "Thirain", "Gienah" },
        { "Kayangel", "Gienah" },
        { "Galatur", "Luterra" },
        { "Azena", "Luterra" },
        { "Ladon", "Balthorr" },
        { "Una", "Balthorr" },
        { "Karta", "Nineveh" },
        { "Azakiel", "Nineveh" },
        { "Avesta", "Inanna" },
        { "Regulus", "Inanna" },
        { "Rohendel", "Thaemine" },
        { "Mari", "Thaemine" },
        { "Valtan", "Brelshaza" },
        { "Lauriel", "Brelshaza" },
        { "Arthetine", "Vairgrys" },
        { "Elgacia", "Vairgrys" },
    };

    public static bool HasMergedServer(string server, [NotNullWhen(true)] out string? mergedServer)
    {
        mergedServer = null;
        while (ServerMerges.TryGetValue(server, out var newServer))
        {
            server = mergedServer = newServer;
        }
        return !string.IsNullOrWhiteSpace(mergedServer);
    }
}
