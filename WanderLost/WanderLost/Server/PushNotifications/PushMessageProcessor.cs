using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using WanderLost.Server.Controllers;
using WanderLost.Server.Data;
using WanderLost.Shared.Data;

namespace WanderLost.Server.PushNotifications;

public class PushMessageProcessor
{
    private const int FirebaseBroadcastLimit = 500;

    private static readonly Counter SentFirebaseNotifications = Metrics.CreateCounter("lostmerchants_sent_firebase_notifications", "Number of push notifications sent via firebase.");
    private static readonly Counter FailedFirebaseNotifications = Metrics.CreateCounter("lostmerchants_failed_firebase_notifications", "Number of push notifications that failed to send via firebase.");

    private readonly ILogger<PushMessageProcessor> _logger;
    private readonly MerchantsDbContext _merchantContext;
    private readonly DataController _dataController;
    private readonly IConfiguration _configuration;

    public PushMessageProcessor(ILogger<PushMessageProcessor> logger, MerchantsDbContext merchantDbContext, DataController dataController, IConfiguration configuration)
    {
        _logger = logger;
        _merchantContext = merchantDbContext;
        _dataController = dataController;
        _configuration = configuration;
    }

    public async Task SendTestNotifications(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested) return;

        var testSubcsriptions = await _merchantContext.PushSubscriptions
            .TagWithCallSite()
            .Where(s => s.SendTestNotification)
            .ToListAsync(stoppingToken);

        foreach (var chunk in testSubcsriptions.Chunk(FirebaseBroadcastLimit))
        {
            if (stoppingToken.IsCancellationRequested) return;

            var message = new MulticastMessage()
            {
                Tokens = chunk.Select(s => s.Token).ToList(),
                Webpush = new WebpushConfig()
                {
                    FcmOptions = new WebpushFcmOptions()
                    {
                        Link = _configuration["IdentityServerOrigin"],
                    },
                    Notification = new WebpushNotification()
                    {
                        Title = $"Test push notification from LostMerchants",
                        Body = $"Test Message Body sent at {DateTime.UtcNow} UTC",
                        Icon = "/images/notifications/ExclamationMark.png",
                        Tag = "test",
                        Renotify = true,
                        Vibrate = [500, 100, 500, 100, 500],
                    },
                    Headers = new Dictionary<string, string>()
                    {
                        { "TTL", "600" },
                        { "Urgency", "high" }
                    }
                },
                Android = new AndroidConfig()
                {
                    Notification = new AndroidNotification()
                    {
                        Title = $"Test push notification from LostMerchants",
                        Body = $"Test Message Body sent at {DateTime.UtcNow} UTC",
                        Tag = "wei",
                        ChannelId = "wei",
                        EventTimestamp = DateTime.Now,
                        Priority = NotificationPriority.MAX,
                        ClickAction = "OPEN_LOSTMERCHANTS_BROWSER",
                    },
                    Priority = Priority.High,
                    TimeToLive = TimeSpan.FromSeconds(600)
                }
            };

            _logger.LogInformation("Sending {attemptCount} test FCM messages.", chunk.Length);

            var batchResponse = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message, stoppingToken);

            if (batchResponse.SuccessCount > 0)
            {
                _logger.LogInformation("{successCount} successful test messages sent.", batchResponse.SuccessCount);
            }
            if (batchResponse.FailureCount > 0)
            {
                _logger.LogInformation("{failureCount} test messages failed to send.", batchResponse.FailureCount);
            }

            foreach (var (subscription, response) in chunk.Zip(batchResponse.Responses))
            {
                if (!response.IsSuccess && response.Exception is FirebaseMessagingException firebaseException)
                {
                    subscription.ConsecutiveFailures++;
                    _logger.LogInformation("Subscription '{subscriptionId}' failed. Error code: {firebaseErrorCode}", subscription.Id, firebaseException.MessagingErrorCode);
                    switch (firebaseException.MessagingErrorCode)
                    {
                        case MessagingErrorCode.InvalidArgument:
#if DEBUG
                            //Invalid could be due to the server code/config, but could also be an invalid token
                            //If we get here in debug, throw, otherwise fall-through and remove the registration
                            throw response.Exception;
#endif
                        case MessagingErrorCode.SenderIdMismatch:
                        case MessagingErrorCode.Unregistered:
                            //Push token no longer valid or is for another FCM subscription, remove from server
                            _merchantContext.Entry(subscription).State = EntityState.Deleted;
                            break;
                        case MessagingErrorCode.Internal:
                        case MessagingErrorCode.Unavailable:
                        case MessagingErrorCode.ThirdPartyAuthError:
                        case MessagingErrorCode.QuotaExceeded:
                        default:
                            //If a "test" message fails, always clear the flag, otherwise these could loop forever
                            _logger.LogWarning(firebaseException, "FCM send failed");
                            subscription.SendTestNotification = false;
                            break;
                    }
                }
                else
                {
                    subscription.ConsecutiveFailures = 0;
                    subscription.SendTestNotification = false;
                }
            }

            await _merchantContext.SaveChangesAsync(stoppingToken);

            //Detach the sent subscriptions
            foreach (var subcsription in chunk)
            {
                _merchantContext.Entry(subcsription).State = EntityState.Detached;
            }
        }

        _merchantContext.ChangeTracker.Clear();
    }

    public async Task ProcessMerchant(ActiveMerchant merchant)
    {
        var cardsToCheck = merchant.Cards.Select(c => c.Name).ToList();
        //First check cards for notifications
        var cardSubscriptions = await _merchantContext.PushSubscriptions
            .TagWithCallSite()
            .Where(s => s.Server == merchant.ActiveMerchantGroup.Server)
            .Where(d => d.CardNotifications.Any(cn => cardsToCheck.Any(c => cn.CardName == c)))
            .Where(s => !_merchantContext.SentPushNotifications.Any(sent => sent.Merchant == merchant && sent.SubscriptionId == s.Id))
            .Where(s => s.CardVoteThreshold <= merchant.Votes)
            .ToListAsync();

        await SendSubscriptionMessages(merchant, cardSubscriptions, MessageType.Card);

        if (merchant.Rapports.Max(r => r.Rarity) >= Rarity.Legendary)
        {
            //Rapport notifications will only be sent for a subscription if it didn't already get notified for a card
            var rapportSubscriptions = await _merchantContext.PushSubscriptions
                .TagWithCallSite()
                .Where(s => s.Server == merchant.ActiveMerchantGroup.Server)
                .Where(s => s.LegendaryRapportNotify)
                .Where(s => !_merchantContext.SentPushNotifications.Any(sent => sent.Merchant == merchant && sent.SubscriptionId == s.Id))
                .Where(s => s.RapportVoteThreshold <= merchant.Votes)
                .ToListAsync();

            await SendSubscriptionMessages(merchant, rapportSubscriptions, MessageType.Rapport);
        }

        if (merchant.MiscItems.Any(i => i.Name == "Stabilized Ductility Catalyst"))
        {
            var catalystSubscriptions = await _merchantContext.PushSubscriptions
                .TagWithCallSite()
                .Where(s => s.Server == merchant.ActiveMerchantGroup.Server)
                .Where(s => s.CatalystNotification)
                .Where(s => !_merchantContext.SentPushNotifications.Any(sent => sent.Merchant == merchant && sent.SubscriptionId == s.Id))
                .Where(s => s.RapportVoteThreshold <= merchant.Votes)
                .ToListAsync();

            if (catalystSubscriptions.Any())
            {
                await SendSubscriptionMessages(merchant, catalystSubscriptions, MessageType.Elixir);
            }
        }
    }

    private async Task SendSubscriptionMessages(ActiveMerchant merchant, List<PushSubscription> subcriptions, MessageType messageType)
    {
        var message = await BuildMulticast(merchant, messageType);

        foreach (var chunk in subcriptions.Chunk(FirebaseBroadcastLimit))
        {
            message.Tokens = chunk.Select(s => s.Token).ToList();

            _logger.LogInformation("Sending {attemptCount} FCM messages for merchant {merchantId}.", chunk.Length, merchant.Id);

            var batchResponse = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);

            if (batchResponse.SuccessCount > 0)
            {
                SentFirebaseNotifications.Inc(batchResponse.SuccessCount);
                _logger.LogInformation("{successCount} successful messages sent.", batchResponse.SuccessCount);
            }
            if (batchResponse.FailureCount > 0)
            {
                FailedFirebaseNotifications.Inc(batchResponse.FailureCount);
                _logger.LogInformation("{failureCount} messages failed to send.", batchResponse.FailureCount);
            }

            var sentNotifications = new List<SentPushNotification>(chunk.Length);

            foreach (var (subscription, response) in chunk.Zip(batchResponse.Responses))
            {
                if (!response.IsSuccess && response.Exception is FirebaseMessagingException firebaseException)
                {
                    subscription.ConsecutiveFailures++;
                    _logger.LogInformation("Subscription '{subscriptionId}' failed. Error code: {firebaseErrorCode}", subscription.Id, firebaseException.MessagingErrorCode);
                    switch (firebaseException.MessagingErrorCode)
                    {
                        case MessagingErrorCode.Internal:
                        case MessagingErrorCode.Unavailable:
                        case MessagingErrorCode.QuotaExceeded:
                            //Ignore and retry later?
                            break;

                        case MessagingErrorCode.ThirdPartyAuthError:
                            //Seems to happen intermittently
                            _logger.LogWarning(firebaseException, "FCM send failed");
                            break;

                        case MessagingErrorCode.InvalidArgument:
#if DEBUG
                            //Invalid could be due to the server code/config, but could also be an invalid token from client
                            //If we get here in debug, throw, otherwise fall-through and remove the registration
                            throw response.Exception;
#endif
                        case MessagingErrorCode.SenderIdMismatch:
                        case MessagingErrorCode.Unregistered:
                            //Push token no longer valid or is for another FCM subscription, remove from server
                            _merchantContext.Entry(subscription).State = EntityState.Deleted;
                            break;

                        default:
                            sentNotifications.Add(new SentPushNotification()
                            {
                                Merchant = merchant,
                                SubscriptionId = subscription.Id,
                            });
                            break;
                    }
                    if (subscription.ConsecutiveFailures > 100 &&
                        (firebaseException.MessagingErrorCode == MessagingErrorCode.ThirdPartyAuthError || firebaseException.MessagingErrorCode == MessagingErrorCode.Internal))
                    {
                        //If a susbscription is consistently failing due to third party errors, purge it
                        _merchantContext.Entry(subscription).State = EntityState.Deleted;
                        _logger.LogInformation("Purging subscription '{subscription.Id}' due to repeated failures.", subscription.Id);
                    }
                }
                else
                {
                    subscription.ConsecutiveFailures = 0;
                    sentNotifications.Add(new SentPushNotification()
                    {
                        Merchant = merchant,
                        SubscriptionId = subscription.Id,
                    });
                }
            }

            await _merchantContext.SentPushNotifications.AddRangeAsync(sentNotifications);

            await _merchantContext.SaveChangesAsync();
        }
    }

    private enum MessageType
    {
        Card,
        Rapport,
        Elixir,
    }

    private async Task<MulticastMessage> BuildMulticast(ActiveMerchant merchant, MessageType messageType)
    {
        string region = (await _dataController.GetMerchantData())[merchant.Name].Region;
        int ttl = Math.Max(60 * (55 - DateTime.Now.Minute + 1), 60);
        var topCard = merchant.Cards.MaxBy(c => c.Rarity) ?? new();
        var title = string.Empty;
        switch (messageType)
        {
            case MessageType.Card:
                title = $"{topCard.Name} Card";
                break;
            case MessageType.Rapport:
                title = "Legendary Rapport";
                break;
            case MessageType.Elixir:
                title = "Elixir Catalyst";
                break;
        }
        return new MulticastMessage()
        {
            Webpush = new WebpushConfig()
            {
                FcmOptions = new WebpushFcmOptions()
                {
                    Link = _configuration["IdentityServerOrigin"],
                },
                Notification = new WebpushNotification()
                {
                    Title = title,
                    Body = $"{region}",
                    Icon = "/images/notifications/ExclamationMark.png",
                    Tag = messageType == MessageType.Card ? "wei" : "rapport",
                    Renotify = true,
                    Vibrate = [500, 100, 500, 100, 500],
                },
                Headers = new Dictionary<string, string>()
                {
                    { "TTL", ttl.ToString() },
                    { "Urgency", "high" },
                }
            },
            Android = new AndroidConfig()
            {
                Notification = new AndroidNotification()
                {
                    Title = title,
                    Body = $"{region}",
                    Tag = messageType == MessageType.Card ? "wei" : "rapport",
                    ChannelId = messageType == MessageType.Card ? "wei" : "rapport",
                    EventTimestamp = DateTime.Now,
                    Priority = messageType == MessageType.Card ? NotificationPriority.MAX : NotificationPriority.HIGH,
                    ClickAction = "OPEN_LOSTMERCHANTS_BROWSER",
                },
                Priority = Priority.High,
                TimeToLive = TimeSpan.FromSeconds(ttl)
            }
        };
    }
}
