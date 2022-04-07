using Microsoft.EntityFrameworkCore;
using WanderLost.Shared.Data;

namespace WanderLost.Server.Controllers
{
    public class MerchantsDbContext : DbContext
    {
        public MerchantsDbContext(DbContextOptions<MerchantsDbContext> options) : base(options)
        {

        }

        public DbSet<ActiveMerchantGroup> MerchantGroups { get; set; } = default!;
        public DbSet<ActiveMerchant> ActiveMerchants { get; set; } = default!;
        public DbSet<Vote> Votes { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActiveMerchantGroup>()
                .HasAlternateKey(g => new { g.Server, g.MerchantName, g.AppearanceExpires });

            modelBuilder.Entity<Vote>()
                .HasKey(v => new { v.ActiveMerchantId, v.ClientId });
        }
    }
}
