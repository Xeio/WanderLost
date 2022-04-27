using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WanderLost.Server.Data;

namespace WanderLost.Server.Controllers
{
    public class AuthDbContext : ApiAuthorizationDbContext<WanderlostUser>, IDataProtectionKeyContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options, IOptions<OperationalStoreOptions> operationalStoreOptions) 
            : base(options, operationalStoreOptions)
        {
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
    }
}
