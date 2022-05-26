using System.ComponentModel.DataAnnotations;

namespace WanderLost.Server.Data;

public class Ban
{
    [MaxLength(60)]
    public string ClientId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset ExpiresAt { get; set; }
}
