using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using WanderLost.Server.Controllers;
using WanderLost.Server.Discord.Data;
using WanderLost.Shared.Data;

namespace WanderLost.Server.Discord;

public class DiscordPushProcessor
{
    private readonly ILogger<DiscordPushProcessor> _logger;
    private readonly MerchantsDbContext _merchantContext;
    private readonly DataController _dataController;
    private readonly IConfiguration _configuration;
    private readonly DiscordSocketClient _discordClient;

    public DiscordPushProcessor(ILogger<DiscordPushProcessor> logger, MerchantsDbContext merchantDbContext, DataController dataController, DiscordSocketClient discordClient, IConfiguration configuration)
    {
        _logger = logger;
        _merchantContext = merchantDbContext;
        _dataController = dataController;
        _configuration = configuration;
        _discordClient = discordClient;
    }

    public async Task SendTestNotifications(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested) return;

        var testSubscriptions = await _merchantContext.DiscordNotifications
            .TagWithCallSite()
            .Where(s => s.SendTestNotification)
            .ToListAsync(stoppingToken);

        if (!testSubscriptions.Any()) return;

        if (stoppingToken.IsCancellationRequested) return;

        _logger.LogInformation("Sending {attemptCount} test discord messages.", testSubscriptions.Count);

        try
        {
            foreach (var subscription in testSubscriptions)
            {

                try
                {
                    var user = await _discordClient.GetUserAsync(subscription.UserId);
                    if (user is null)
                    {
                        _logger.LogWarning("Unable to get user {UserId} for test notification", subscription.UserId);
                        continue;
                    }
                    await user.SendMessageAsync("Test notification for Lost Merchants");
                }
                catch (HttpException e) when (e.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
                {
                    _logger.LogInformation("Unable to send test message to discord user {UserId}", subscription.UserId);
                }
                finally
                {
                    subscription.SendTestNotification = false;
                }
            }
        }
        finally
        {
            await _merchantContext.SaveChangesAsync(stoppingToken);
        }

        _merchantContext.ChangeTracker.Clear();
    }

    public async Task ProcessMerchant(ActiveMerchant merchant)
    {
        if (merchant.Votes < 0 || merchant.Hidden ||
            merchant.ActiveMerchantGroup.AppearanceExpires < DateTimeOffset.Now)
        {
            //Don't need to send notifications for downvoted/hidden/expired merchants
            return;
        }

        //First check cards for notifications
        var cardSubscriptions = await _merchantContext.DiscordNotifications
            .TagWithCallSite()
            .AsNoTracking()
            .Where(d => d.Server == merchant.ActiveMerchantGroup.Server)
            .Where(d => d.CardNotifications.Any(c => c.CardName == merchant.Card.Name))
            .Where(d => !_merchantContext.SentDiscordNotifications.Any(sent => sent.MerchantId == merchant.Id && sent.DiscordNotificationUserId == d.UserId))
            .Where(d => d.CardVoteThreshold <= merchant.Votes)
            .ToListAsync();

        if (cardSubscriptions.Any())
        {
            await SendSubscriptionMessages(merchant, cardSubscriptions);
        }
    }

    private async Task SendSubscriptionMessages(ActiveMerchant merchant, List<DiscordNotification> subcriptions)
    {
        var embed = await BuildMerchantMessage(merchant);

        _logger.LogInformation("Sending {attemptCount} Discord messages for merchant {merchantId}.", subcriptions.Count, merchant.Id);

        foreach (var chunk in subcriptions.Chunk(20))
        {
            try
            {
                foreach (var subscription in chunk)
                {
                    var user = await _discordClient.GetUserAsync(subscription.UserId);
                    if (user is null)
                    {
                        _logger.LogWarning("Unable to get user {UserId} for merchant notification", subscription.UserId);
                        continue;
                    }
                    try
                    {
                        await user.SendMessageAsync(embed: embed);

                        await _merchantContext.SentDiscordNotifications.AddAsync(new SentDiscordNotification()
                        {
                            MerchantId = merchant.Id,
                            DiscordNotificationUserId = subscription.UserId,
                        });
                    }
                    catch(HttpException e) when (e.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
                    {
                        //User disallows DMs or the bot otherwise can't send the message, purge the subscription
                        _logger.LogInformation("Unable to message discord user {UserId}, purging subscription.", subscription.UserId);
                        _merchantContext.Entry(subscription).State = EntityState.Deleted;
                    }
                }
            }
            finally
            {
                //Make sure we record any sent responses
                await _merchantContext.SaveChangesAsync();
            }
        }
    }

    private async Task<Embed> BuildMerchantMessage(ActiveMerchant merchant)
    {
        string region = (await _dataController.GetMerchantData())[merchant.Name].Region;

        var embed = new EmbedBuilder()
        {
            Title = $"{merchant.Card.Name} Card - {region} - {merchant.Zone}",
            Url = _configuration["IdentityServerOrigin"],
            Color = merchant.Card.Rarity == Rarity.Legendary ? Color.Gold : Color.DarkPurple,
        };
        embed.AddField("Card", merchant.Card.Name);
        embed.AddField("Region", region);
        embed.AddField("Zone", merchant.Zone);
        embed.AddField("Spawn expires", TimestampTag.FormatFromDateTimeOffset(merchant.ActiveMerchantGroup.AppearanceExpires, TimestampTagStyles.Relative));

        return embed.Build();
    }
}
