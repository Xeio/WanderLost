using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WanderLost.Server.Data;
using WanderLost.Server.Discord.Data;
using WanderLost.Shared.Data;

namespace WanderLost.Server.Controllers;

public class MerchantsDbContext : ApiAuthorizationDbContext<WanderlostUser>, IDataProtectionKeyContext
{
    public MerchantsDbContext(DbContextOptions<MerchantsDbContext> options, IOptions<OperationalStoreOptions> operationalStoreOptions)
        : base(options, operationalStoreOptions)
    {
    }

    public DbSet<ActiveMerchantGroup> MerchantGroups { get; set; } = default!;
    public DbSet<ActiveMerchant> ActiveMerchants { get; set; } = default!;
    public DbSet<Vote> Votes { get; set; } = default!;
    public DbSet<Ban> Bans { get; set; } = default!;
    public DbSet<PushSubscription> PushSubscriptions { get; set; } = default!;
    public DbSet<CardNotification> CardNotifications { get; set; } = default!;
    public DbSet<SentPushNotification> SentPushNotifications { get; set; } = default!;
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
    public DbSet<LeaderboardEntry> Leaderboards { get; set; } = default!;
    public DbSet<DiscordNotification> DiscordNotifications { get; set; } = default!;
    public DbSet<DiscordCardNotification> DiscordCardNotifications { get; set; } = default!;
    public DbSet<SentDiscordNotification> SentDiscordNotifications { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ActiveMerchantGroup>()
            .HasAlternateKey(g => new { g.AppearanceExpires, g.Server, g.MerchantName });

        modelBuilder.Entity<ActiveMerchant>()
            .HasIndex(g => g.UploadedByUserId);

        modelBuilder.Entity<ActiveMerchant>()
            .HasIndex(m => new { m.RequiresProcessing })
            .HasFilter($"[{nameof(ActiveMerchant.RequiresProcessing)}] = 1");

        modelBuilder.Entity<ActiveMerchant>()
            .HasIndex(m => new { m.RequiresVoteProcessing })
            .HasFilter($"[{nameof(ActiveMerchant.RequiresVoteProcessing)}] = 1");

        modelBuilder.Entity<ActiveMerchant>()
            .HasIndex(m => new { m.PostProcessComplete })
            .HasFilter($"[{nameof(ActiveMerchant.PostProcessComplete)}] = 0");

        modelBuilder.Entity<ActiveMerchant>()
            .HasIndex(m => new { m.RequiresLeaderboardProcessing })
            .HasFilter($"[{nameof(ActiveMerchant.RequiresLeaderboardProcessing)}] = 1");

        modelBuilder.Entity<Ban>()
            .HasKey(b => new { b.ClientId, b.ExpiresAt });

        modelBuilder.Entity<PushSubscription>()
            .HasAlternateKey(p => p.Token);

        modelBuilder.Entity<PushSubscription>()
            .HasIndex(p => new { p.Server });

        modelBuilder.Entity<PushSubscription>()
            .HasIndex(p => new { p.SendTestNotification })
            .HasFilter($"[{nameof(PushSubscription.SendTestNotification)}] = 1");

        modelBuilder.Entity<SentPushNotification>()
            .HasKey(sn => new { sn.MerchantId, sn.SubscriptionId });

        modelBuilder.Entity<SentPushNotification>()
            .HasOne<PushSubscription>()
            .WithMany()
            .HasForeignKey(sn => sn.SubscriptionId);

        modelBuilder.Entity<CardNotification>()
            .HasKey(c => new { c.PushSubscriptionId, c.CardName });

        modelBuilder.Entity<CardNotification>()
            .HasIndex(c => c.CardName);

        modelBuilder.Entity<DiscordNotification>()
            .HasIndex(d => new { d.Server });

        modelBuilder.Entity<DiscordNotification>()
            .HasIndex(d => new { d.SendTestNotification })
            .HasFilter($"[{nameof(DiscordNotification.SendTestNotification)}] = 1");

        modelBuilder.Entity<DiscordCardNotification>()
            .HasKey(d => new { d.DiscordNotificationUserId, d.CardName });

        modelBuilder.Entity<DiscordCardNotification>()
            .HasIndex(d => d.CardName);

        modelBuilder.Entity<SentDiscordNotification>()
            .HasKey(sd => new { sd.MerchantId, sd.DiscordNotificationUserId });

        modelBuilder.Entity<SentDiscordNotification>()
            .HasOne<DiscordNotification>()
            .WithMany()
            .HasForeignKey(dn => dn.DiscordNotificationUserId);
    }
}
