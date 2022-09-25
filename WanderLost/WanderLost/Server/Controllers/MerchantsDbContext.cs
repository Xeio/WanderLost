﻿using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WanderLost.Server.Data;
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
    public DbSet<SentPushNotification> SentPushNotifications { get; set; } = default!;
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;
    public DbSet<LeaderboardEntry> Leaderboards { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ActiveMerchantGroup>()
            .HasAlternateKey(g => new { g.Server, g.MerchantName, g.AppearanceExpires });

        modelBuilder.Entity<ActiveMerchant>()
            .HasIndex(g => g.UploadedBy);

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
            .HasAlternateKey(p => new { p.Id });

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
            .HasForeignKey(sn => sn.SubscriptionId)
            .HasPrincipalKey(s => s.Id);
    }
}
