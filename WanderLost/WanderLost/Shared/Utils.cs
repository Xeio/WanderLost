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

    private static readonly Dictionary<string, string> _serverMerges = new()
    {
        { "Shadespire", "Rethramis" },
        { "Petrania", "Tortoyk" },
        { "Tragon", "Moonkeep" },
        { "Stonehearth", "Punika" },
        { "Kurzan", "Agaton" },
        { "Prideholme", "Vern" },
        { "Yorn", "Gienah" },
        { "Feiton", "Arcturus" },
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
        //Fix misspelling
        { "Eyalyn", "Ealyn" },
    };

    public static bool HasMergedServer(string server, [NotNullWhen(true)] out string? mergedServer)
    {
        if (_serverMerges.TryGetValue(server, out var newServer))
        {
            mergedServer = newServer;
            if (_serverMerges.TryGetValue(newServer, out var secondaryServer))
            {
                mergedServer = secondaryServer;
            }
            return true;
        }
        mergedServer = null;
        return false;
    }
}
