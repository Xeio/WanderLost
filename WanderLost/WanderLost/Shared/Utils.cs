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
        { "Shadespire", "Ealyn" },
        { "Petrania", "Ealyn" },
        { "Tragon", "Nia" },
        { "Stonehearth", "Nia" },
        { "Kurzan", "Arthetine" },
        { "Prideholme", "Arthetine" },
        { "Yorn", "Blackfang" },
        { "Feiton", "Blackfang" },
        { "Sirius", "Armen" },
        { "Sceptrum", "Armen" },
        { "Thaemine", "Lazenith" },
        { "Procyon", "Lazenith" },
        { "Nineveh", "Evergrace" },
        { "Beatrice", "Evergrace" },
        { "Brelshaza", "Ezrebet" },
        { "Inanna", "Ezrebet" },
        { "Rethramis", "Ealyn" },
        { "Tortoyk", "Ealyn" },
        { "Moonkeep", "Nia" },
        { "Punika", "Nia" },
        { "Agaton", "Arthetine" },
        { "Vern", "Arthetine" },
        { "Gienah", "Blackfang" },
        { "Arcturus", "Blackfang" },
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
