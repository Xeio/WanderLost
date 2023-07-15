using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using WanderLost.Server.Controllers;
using WanderLost.Server.Discord;

namespace WanderLost.Server.PushNotifications;

public class PushWorkerService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PushWorkerService> _logger;

    public PushWorkerService(ILogger<PushWorkerService> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

            if (stoppingToken.IsCancellationRequested) return;

            using var scope = _services.CreateScope();

            var firebasePushProcessor = scope.ServiceProvider.GetRequiredService<PushMessageProcessor>();
            var discordProcessor = scope.ServiceProvider.GetService<DiscordPushProcessor>();
            var merchantContext = scope.ServiceProvider.GetRequiredService<MerchantsDbContext>();

            if (FirebaseAdmin.FirebaseApp.DefaultInstance is null)
            {
                _logger.LogCritical("Firebase not configured, skipping firebase message sending. Need 'FirebaseSecretFile' config setting for private key.");
            }
            if(discordProcessor is null)
            {
                _logger.LogCritical("Discord push message processor is not resolved, missing 'DiscordBotToken' config.");
            }

            try
            {
                if (FirebaseAdmin.FirebaseApp.DefaultInstance is not null)
                {
                    await firebasePushProcessor.SendTestNotifications(stoppingToken);
                }
                if (discordProcessor is not null)
                {
                    await discordProcessor.SendTestNotifications(stoppingToken);
                }

                var merchants = await merchantContext.ActiveMerchants
                    .TagWithCallSite()
                    .Where(m => m.RequiresProcessing)
                    .Include(m => m.ActiveMerchantGroup)
                    .ToListAsync(stoppingToken);

                foreach (var merchant in merchants)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    if (FirebaseAdmin.FirebaseApp.DefaultInstance is not null)
                    {
                        await firebasePushProcessor.ProcessMerchant(merchant);
                    }
                    if (discordProcessor is not null)
                    {
                        await discordProcessor.ProcessMerchant(merchant);
                    }

                    merchant.RequiresProcessing = false;
                }

                await merchantContext.SaveChangesAsync(stoppingToken);
            }
            catch (FirebaseMessagingException e)
            {
                //If for some reason the overall requests fail, log error and just try again next polling period
                _logger.LogError(e, "Communication failure with Firebase.");
            }
        }
    }
}
