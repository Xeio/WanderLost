using Microsoft.AspNetCore.Identity;

namespace WanderLost.Server.Data;

public class WanderlostUser : IdentityUser
{
    public DateTimeOffset? BanExpires { get; set; }
    public DateTimeOffset? BannedAt { get; set; }
}
