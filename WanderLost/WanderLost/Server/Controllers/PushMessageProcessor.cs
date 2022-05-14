using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using WanderLost.Server.Data;
using WanderLost.Shared.Data;

namespace WanderLost.Server.Controllers
{
    public class PushMessageProcessor
    {
        private const int FirebaseBroadcastLimit = 500;

        private readonly ILogger<PushMessageProcessor> _logger;
        private readonly MerchantsDbContext _merchantContext;
        private readonly DataController _dataController;

        public PushMessageProcessor(ILogger<PushMessageProcessor> logger, MerchantsDbContext merchantDbContext, DataController dataController)
        {
            _logger = logger;
            _merchantContext = merchantDbContext;
            _dataController = dataController;
        }

        public async Task SendTestNotifications(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested) return;

            var testSubcsriptions = await _merchantContext.PushSubscriptions
                .TagWithCallSite()
                .Where(s => s.SendTestNotification)
                .ToListAsync(stoppingToken);

            foreach(var chunk in Enumerable.Chunk(testSubcsriptions, FirebaseBroadcastLimit))
            {
                if (stoppingToken.IsCancellationRequested) return;

                var message = new MulticastMessage()
                {
                    Tokens = chunk.Select(s => s.Token).ToList(),
                    Webpush = new WebpushConfig()
                    {
                        Notification = new WebpushNotification()
                        {
                            Title = $"Test push notification from LostMerchants",
                            Body = $"Test Message Body sent at {DateTime.UtcNow} UTC",
                            Icon = "/images/notifications/ExclamationMark.png",
                            Tag = "test",
                            Renotify = true,
                            Vibrate = new[] { 500, 100, 500, 100, 500 },
                            Actions = new[]{ new FirebaseAdmin.Messaging.Action()
                                {
                                     ActionName = "openSite",
                                      Title = "Open LostMerchants"
                                } 
                            }
                        },
                        Headers = new Dictionary<string, string>()
                        {
                            { "TTL", "600" },
                            { "Urgency", "high" }
                        }
                    }
                };

                _logger.LogInformation("Sending {attemptCount} test FCM messages.", chunk.Length);

                var batchResponse = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message, stoppingToken);

                if(batchResponse.SuccessCount > 0)
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
                        switch (firebaseException.MessagingErrorCode)
                        {
                            case MessagingErrorCode.Internal:
                            case MessagingErrorCode.Unavailable:
                                //Ignore and retry later?
                                break;

                            case MessagingErrorCode.ThirdPartyAuthError:
                                //Should probably only happen because of code or config issues
                                _logger.LogCritical(firebaseException, "FCM send failed");
                                if (batchResponse.SuccessCount == 0)
                                {
                                    return;
                                }
                                break;

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

                            case MessagingErrorCode.QuotaExceeded:
                            default:
                                subscription.SendTestNotification = false;
                                break;
                        }
                    }
                    else
                    {
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

        public async Task RunMerchantUpdates(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested) return;

            //Process any merchants in need of update
            var merchants = await _merchantContext.ActiveMerchants
                .TagWithCallSite()
                .Where(m => m.RequiresProcessing)
                .Include(m => m.ActiveMerchantGroup)
                .ToListAsync(stoppingToken);

            foreach (var merchant in merchants)
            {
                if (stoppingToken.IsCancellationRequested) return;

                await ProcessMerchant(merchant);
            }
        }

        private async Task ProcessMerchant(ActiveMerchant merchant)
        {
            if (merchant.Votes < 0 || merchant.Hidden)
            {
                //Don't need to send notifications for downvoted merchants
                merchant.RequiresProcessing = false;
                await _merchantContext.SaveChangesAsync();
                _merchantContext.Entry(merchant).State = EntityState.Detached;
                return;
            }

            if (merchant.Card.Name == "Wei")
            {
                var weiSubscriptions = await _merchantContext.PushSubscriptions
                    .TagWithCallSite()
                    .AsNoTracking()
                    .Where(s => s.Server == merchant.ActiveMerchantGroup.Server)
                    .Where(s => s.WeiNotify && !_merchantContext.SentPushNotifications.Any(sent => sent.Merchant == merchant && sent.SubscriptionId == s.Id))
                    .Where(s => s.WeiVoteThreshold <= merchant.Votes)
                    .ToListAsync();

                await SendSubscriptionMessages(merchant, weiSubscriptions);
            }
            else if (merchant.Rapport.Rarity >= Rarity.Legendary)
            {
                var rapportSubscriptions = await _merchantContext.PushSubscriptions
                    .TagWithCallSite()
                    .AsNoTracking()
                    .Where(s => s.Server == merchant.ActiveMerchantGroup.Server)
                    .Where(s => s.LegendaryRapportNotify && !_merchantContext.SentPushNotifications.Any(sent => sent.Merchant == merchant && sent.SubscriptionId == s.Id))
                    .Where(s => s.RapportVoteThreshold <= merchant.Votes)
                    .ToListAsync();

                await SendSubscriptionMessages(merchant, rapportSubscriptions);
            }

            merchant.RequiresProcessing = false;
            _merchantContext.SaveChanges();
            _merchantContext.Entry(merchant).State = EntityState.Detached;
        }

        private async Task SendSubscriptionMessages(ActiveMerchant merchant, List<PushSubscription> subcriptions)
        {
            var message = await BuildMulticast(merchant);

            foreach (var chunk in subcriptions.Chunk(FirebaseBroadcastLimit))
            {
                message.Tokens = chunk.Select(s => s.Token).ToList();

                _logger.LogInformation("Sending {attemptCount} FCM messages.", chunk.Length);

                var batchResponse = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);

                if (batchResponse.SuccessCount > 0)
                {
                    _logger.LogInformation("{successCount} successful messages sent.", batchResponse.SuccessCount);
                }
                if (batchResponse.FailureCount > 0)
                {
                    _logger.LogInformation("{failureCount} messages failed to send.", batchResponse.FailureCount);
                }

                var sentNotifications = new List<SentPushNotification>(chunk.Length);

                foreach (var (subscription, response) in chunk.Zip(batchResponse.Responses))
                {
                    if (!response.IsSuccess && response.Exception is FirebaseMessagingException firebaseException)
                    {
                        switch (firebaseException.MessagingErrorCode)
                        {
                            case MessagingErrorCode.Internal:
                            case MessagingErrorCode.Unavailable:
                            case MessagingErrorCode.QuotaExceeded:
                                //Ignore and retry later?
                                break;

                            case MessagingErrorCode.ThirdPartyAuthError:
                                //Should probably only happen because of code or config issues
                                _logger.LogCritical(firebaseException, "FCM send failed");
                                if (batchResponse.SuccessCount == 0)
                                {
                                    return;
                                }
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
                    }
                    else
                    {
                        sentNotifications.Add(new SentPushNotification()
                        {
                            Merchant = merchant,
                            SubscriptionId = subscription.Id,
                        });
                    }
                }

                await _merchantContext.SentPushNotifications.AddRangeAsync(sentNotifications);

                await _merchantContext.SaveChangesAsync();

                foreach (var sentNotification in sentNotifications)
                {
                    _merchantContext.Entry(sentNotification).State = EntityState.Detached;
                }
            }
        }

        private async Task<MulticastMessage> BuildMulticast(ActiveMerchant merchant)
        {
            string region = (await _dataController.GetMerchantData())[merchant.Name].Region;
            int ttl = 60 * (55 - DateTime.Now.Minute + 1);
            bool isWei = merchant.Card.Name == "Wei";
            return new MulticastMessage()
            {
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        Title = isWei ? "Wei card" : "Legendary Rapport",
                        Body = isWei ? "Wei Card!!!" : $"Legendary Rapport - {region}",
                        Icon = "/images/notifications/ExclamationMark.png",
                        Tag = isWei ? "wei" : "rapport",
                        Renotify = true,
                        Vibrate = new[] { 500, 100, 500, 100, 500 },
                        Actions = new[]{ new FirebaseAdmin.Messaging.Action()
                            {
                                ActionName = "openSite",
                                Title = "Open LostMerchants"
                            }
                        }
                    },
                    Headers = new Dictionary<string, string>()
                    {
                        { "TTL", ttl.ToString() },
                        { "Urgency", "high" }
                    }
                }
            };
        }
    }
}
