using FirebaseAdmin.Messaging;

namespace WanderLost.Server.Controllers;

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

            if(FirebaseAdmin.FirebaseApp.DefaultInstance == null)
            {
                _logger.LogCritical("Firebase not configured, skipping message sending. Need 'FirebaseSecretFile' config setting for private key.");
                continue;
            }

            if (stoppingToken.IsCancellationRequested) return;

            using var scope = _services.CreateScope();
            var messageProcessor = scope.ServiceProvider.GetRequiredService<PushMessageProcessor>();

            try
            {
                await messageProcessor.SendTestNotifications(stoppingToken);

                await messageProcessor.RunMerchantUpdates(stoppingToken);
            }
            catch(FirebaseMessagingException e)
            {
                //If for some reason the overall requests fail, log error and just try again next polling period
                _logger.LogError(e, "Communication failure with Firebase.");
            }
        }
    }
}
