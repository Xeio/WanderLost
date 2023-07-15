using Microsoft.EntityFrameworkCore;
using WanderLost.Server.Controllers;
using WanderLost.Server.Discord.Data;

namespace WanderLost.Server.Discord;

public class DiscordSubscriptionManager
{
    private readonly MerchantsDbContext _merchantsContext;

    public DiscordSubscriptionManager(MerchantsDbContext merchantsContext)
    {
        _merchantsContext = merchantsContext;
    }

    public async Task UpdateSubscriptionServer(ulong userId, string server)
    {
        var currentSubscription = await _merchantsContext.DiscordNotifications.FindAsync(userId);

        if (currentSubscription is null)
        {
            var newSubscription = new DiscordNotification()
            {
                UserId = userId,
                Server = server,
            };
            await _merchantsContext.AddAsync(newSubscription);
        }
        else
        {
            currentSubscription.Server = server;
        }
        await _merchantsContext.SaveChangesAsync();
    }

    public async Task UpdateCardVoteThreshold(ulong userId, int votes)
    {
        var currentSubscription = await _merchantsContext.DiscordNotifications.FindAsync(userId);

        if (currentSubscription is not null)
        {
            currentSubscription.CardVoteThreshold = votes;
            await _merchantsContext.SaveChangesAsync();
        }
    }

    public async Task AddCardToSubscription(ulong userId, string cardName)
    {
        var currentSubscription = await _merchantsContext.DiscordNotifications.FindAsync(userId);

        if (currentSubscription is not null)
        {
            if (!currentSubscription.CardNotifications.Any(n => n.CardName == cardName))
            {
                currentSubscription.CardNotifications.Add(new DiscordCardNotification() { CardName = cardName });
                await _merchantsContext.SaveChangesAsync();
            }
        }
    }

    public async Task RemoveCardFromSubscription(ulong userId, string cardName)
    {
        var currentSubscription = await _merchantsContext.DiscordNotifications
            .TagWithCallSite()
            .Include(d => d.CardNotifications)
            .SingleOrDefaultAsync(d => d.UserId == userId);

        if (currentSubscription is not null)
        {
            var cardNotify = currentSubscription.CardNotifications.FirstOrDefault(n => n.CardName == cardName);
            if (cardNotify != null)
            {
                currentSubscription.CardNotifications.Remove(cardNotify);
                await _merchantsContext.SaveChangesAsync();
            }
        }
    }

    public async Task SetSubscriptionTestFlag(ulong userId)
    {
        var currentSubscription = await _merchantsContext.DiscordNotifications.FindAsync(userId);

        if (currentSubscription is null)
        {
            var newSubscription = new DiscordNotification()
            {
                UserId = userId,
                SendTestNotification = true,
            };
            await _merchantsContext.AddAsync(newSubscription);
        }
        else
        {
            currentSubscription.SendTestNotification = true;
        }
        await _merchantsContext.SaveChangesAsync();
    }

    public async Task<DiscordNotification?> GetCurrentSubscription(ulong userId)
    {
        return await _merchantsContext.DiscordNotifications
            .TagWithCallSite()
            .AsNoTracking()
            .Include(d => d.CardNotifications)
            .SingleOrDefaultAsync(d => d.UserId == userId);
    }
}
