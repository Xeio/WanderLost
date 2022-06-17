using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WanderLost.Shared.Data;
using WanderLost.Shared.Interfaces;

namespace WanderLost.Server.Controllers;

public class BanProcessor : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PushWorkerService> _logger;

    public BanProcessor(ILogger<PushWorkerService> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            if (stoppingToken.IsCancellationRequested) return;

            //Run this between :15 and :19 after the hour
            if (DateTime.Now.Minute >= 15 && DateTime.Now.Minute < 20) continue;

            using var scope = _services.CreateScope();
            var merchantDbContext = scope.ServiceProvider.GetRequiredService<MerchantsDbContext>();

            var merchantsToProcess = await merchantDbContext.ActiveMerchants
                .TagWithCallSite()
                .Where(m => !m.PostProcessComplete)
                .ToListAsync(stoppingToken);

            foreach (var merchant in merchantsToProcess)
            {
                await ProcessMerchant(merchant, merchantDbContext, stoppingToken);
                merchant.PostProcessComplete = true;
            }
            await merchantDbContext.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task ProcessMerchant(ActiveMerchant merchant, MerchantsDbContext merchantContext, CancellationToken stoppingToken)
    {
        if (merchant.Hidden || merchant.Votes >= 0 ||
            await AlreadyHasBan(merchant, merchantContext, stoppingToken))
        {
            //Don't need to check for new bans for hidden/positive votes/already banned users
            return;
        }

        bool permaban = false;
        int banDays = 0;

        if (merchant.Card.Name == "Wei" && merchant.Votes < -3)
        {
            var merchants = await GetAssociatedMerchants(merchant, merchantContext, stoppingToken);
            if (merchants.Count(m => m.Card.Name == "Wei" && m.Votes < 0) > 1)
            {
                //More than one bad Wei card
                permaban = true;
            }
            else if(!merchants.Any(m => m.Votes > 0))
            {
                //No positive submissions
                permaban = true;
            }
            else if(merchants.Sum(m => m.Votes) < 0)
            {
                //Total votes on user are negative
                permaban = true;
            }
            else
            {
                banDays = 30;
            }
        }
        else if (merchant.Rapport.Rarity >= Rarity.Legendary && merchant.Votes < -3)
        {
            var merchants = await GetAssociatedMerchants(merchant, merchantContext, stoppingToken);
            int negativeLegendary = merchants.Count(m => m.Rapport.Rarity == Rarity.Legendary && m.Votes < 0);
            int positiveLegendary = merchants.Count(m => m.Rapport.Rarity == Rarity.Legendary && m.Votes > 0);
            if (!merchants.Any(m => m.Votes > 0))
            {
                //No positive submissions
                permaban = true;
            }
            if(positiveLegendary == 0)
            {
                //All the legendary rapports are negative
                banDays = 14;
            }
            if (positiveLegendary > 0 && positiveLegendary < negativeLegendary)
            {
                //More negative legendary rapports than positive
                permaban = true;
            }
            if (merchants.Sum(m => m.Votes) < 0)
            {
                //User's total is negative
                banDays = 30;
            }
            if(merchants
                .Where(m => m.ActiveMerchantGroup.AppearanceExpires > DateTimeOffset.Now.AddDays(-1))
                .Where(m => m.Rapport.Rarity == Rarity.Legendary)
                .Count(m => m.Votes < 0) > 1)
            {
                //Multiple negative submissions in the last day
                banDays = 30;
            }
        }
        else if (string.IsNullOrWhiteSpace(merchant.UploadedByUserId) && merchant.Votes < -2)
        {
            var merchants = await GetAssociatedMerchants(merchant, merchantContext, stoppingToken);
            if(merchants.All(m => m.Votes <= 0) && merchants.Sum(m => m.Votes) < -9)
            {
                //Anonymous user with no positive submissions
                banDays = 14;
            }
        }

        if (permaban || banDays > 0)
        {
            var user = await merchantContext.Users
                .TagWithCallSite()
                .Where(u => u.Id == merchant.UploadedByUserId)
                .FirstOrDefaultAsync();

            if (user is not null)
            {
                if (user.BanExpires is null)
                {
                    user.BanExpires = permaban ? DateTimeOffset.Now.AddYears(100) : DateTimeOffset.Now.AddDays(banDays);
                    user.BannedAt = DateTimeOffset.Now;
                    _logger.LogInformation("Banning user {user} for merchant {merchantId}. Expires: {banExpires}", merchant.UploadedByUserId, merchant.Id, user.BanExpires);
                }
                else if(user.BanExpires < DateTimeOffset.Now)
                {
                    //Banning again after expired ban, permaban
                    user.BanExpires = DateTimeOffset.Now.AddYears(100);
                    user.BannedAt = DateTimeOffset.Now;
                    _logger.LogInformation("Banning user {user} for merchant {merchantId}. Multi permaban.", merchant.UploadedByUserId, merchant.Id);
                }
            }
            else
            {
                var existingBans = await merchantContext.Bans
                    .TagWithCallSite()
                    .AsNoTracking()
                    .Where(b => b.ClientId == merchant.UploadedBy)
                    .ToListAsync();
                if(existingBans.Count(b => b.ExpiresAt > DateTimeOffset.Now) == 0)
                {
                    //Extended ban if IP already has expired bans
                    var extendedBan = existingBans.Count > 0;
                    //Anonymous user, add to IP bans
                    await merchantContext.Bans.AddAsync(new Data.Ban()
                    {
                        ClientId = merchant.UploadedBy,
                        CreatedAt = DateTimeOffset.Now,
                        ExpiresAt = DateTimeOffset.Now.AddDays(extendedBan ? banDays * 2 : banDays),
                    });
                }
            }
        }
    }
    
    public static async Task<List<ActiveMerchant>> GetAssociatedMerchants(ActiveMerchant merchant, MerchantsDbContext merchantContext, CancellationToken stoppingToken)
    {
        if (!string.IsNullOrWhiteSpace(merchant.UploadedByUserId))
        {
            return await merchantContext.ActiveMerchants
                .TagWithCallSite()
                .AsNoTracking()
                .Where(m => !m.Hidden)
                .Where(m => m.UploadedByUserId == merchant.UploadedByUserId)
                .Include(m => m.ActiveMerchantGroup)
                .OrderByDescending(m => m.ActiveMerchantGroup.AppearanceExpires)
                .ToListAsync(stoppingToken);
        }
        return await merchantContext.ActiveMerchants
                .TagWithCallSite()
                .AsNoTracking()
                .Where(m => !m.Hidden)
                .Where(m => m.UploadedBy == merchant.UploadedBy)
                .Include(m => m.ActiveMerchantGroup)
                .OrderByDescending(m => m.ActiveMerchantGroup.AppearanceExpires)
                .ToListAsync(stoppingToken);
    }

    private static async Task<bool> AlreadyHasBan(ActiveMerchant merchant, MerchantsDbContext merchantContext, CancellationToken stoppingToken)
    {
        if (!string.IsNullOrWhiteSpace(merchant.UploadedByUserId))
        {
            return await merchantContext.Users
                .TagWithCallSite()
                .Where(u => u.Id == merchant.UploadedByUserId && u.BanExpires > DateTimeOffset.Now)
                .AnyAsync(stoppingToken);
        }
        return await merchantContext.Bans
            .TagWithCallSite()
            .Where(b => b.ClientId == merchant.UploadedBy && b.ExpiresAt > DateTimeOffset.Now)
            .AnyAsync(stoppingToken);
    }
}
