using System.Text.Json;
using System.Text.Json.Serialization;

namespace WanderLost.Shared
{
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
    }
}
