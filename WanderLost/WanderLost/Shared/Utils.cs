using System.Text.Json;

namespace WanderLost.Shared
{
    public static class Utils
    {
        public static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };
    }
}
