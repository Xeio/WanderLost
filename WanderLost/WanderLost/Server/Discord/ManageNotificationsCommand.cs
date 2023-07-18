using Discord;
using Discord.WebSocket;
using WanderLost.Server.Controllers;
using WanderLost.Server.Discord.Data;

namespace WanderLost.Server.Discord;

public class ManageNotificationsCommand : IDiscordCommand
{
    private readonly DiscordSubscriptionManager _subscriptionManager;
    private readonly DataController _dataController;

    const string MANAGE_NOTIFICATION_COMMAND = "manage-merchant-notifications";
    const string SELECT_REGION_DROPDOWN = "select-region-dropdown";
    const string SELECT_SERVER_DROPDOWN = "select-server-dropdown";
    const string ADD_CARD_DROPDOWN = "add-card-dropdown";
    const string UPDATE_VOTES_MODAL = "update-votes-modal";
    const string UPDATE_VOTES_TEXTINPUT = "update-votes-textinput";
    const string REMOVE_CARD_DROPDOWN = "remove-card-dropdown";
    const string UPDATE_SERVER_BUTTON = "update-server-button";
    const string ADD_CARD_BUTTON = "add-card-button";
    const string REMOVE_CARD_BUTTON = "remove-card-button";
    const string UPDATE_VOTES_BUTTON = "update-votes-button";
    const string REMOVE_ALL_NOTIFICATIONS_BUTTON = "remove-all-notifications-button";

    public ManageNotificationsCommand(DiscordSubscriptionManager subscriptionManager, DataController dataController)
    {
        _subscriptionManager = subscriptionManager;
        _dataController = dataController;
    }

    public SlashCommandProperties CreateCommand()
    {
        var commandBuilder = new SlashCommandBuilder()
        {
            Name = MANAGE_NOTIFICATION_COMMAND,
            Description = "Manage notifications from Lost Merchants",
            IsDMEnabled = true,
        };

        return commandBuilder.Build();
    }

    public async Task ModalSubmitted(SocketModal arg)
    {
        switch (arg.Data.CustomId)
        {
            case UPDATE_VOTES_MODAL:
                {
                    var value = arg.Data.Components.FirstOrDefault(c => c.CustomId == UPDATE_VOTES_TEXTINPUT)?.Value;
                    if(int.TryParse(value, out var votes))
                    {
                        await _subscriptionManager.UpdateCardVoteThreshold(arg.User.Id, votes);
                    }
                    
                    await BuildBasicCommandResponse(arg);

                    break;
                }
        }
    }

    public async Task ButtonExecuted(SocketMessageComponent arg)
    {
        switch (arg.Data.CustomId)
        {
            case UPDATE_SERVER_BUTTON:
                {
                    var regions = await _dataController.GetServerRegions();

                    var select = new SelectMenuBuilder();
                    select.WithPlaceholder("Select server region");
                    select.WithCustomId(SELECT_REGION_DROPDOWN);
                    foreach (var server in regions)
                    {
                        select.AddOption(server.Value.Name, server.Key);
                    }

                    var builder = new ComponentBuilder().WithSelectMenu(select);

                    await arg.RespondAsync("Server Region", components: builder.Build(), ephemeral: true);
                    break;
                }
            case ADD_CARD_BUTTON:
                {
                    var currentSubscription = await _subscriptionManager.GetCurrentSubscription(arg.User.Id);
                    if (currentSubscription is null) return;

                    var cards = await _dataController.GetEpicLegendaryCards();
                    var select = new SelectMenuBuilder()
                    {
                         Placeholder = "Select card to add",
                         CustomId = ADD_CARD_DROPDOWN,
                         MinValues = 1,
                    };
                    foreach (var card in cards
                        .Where(c => !currentSubscription.CardNotifications.Any(n => n.CardName == c.Name))
                        .Select(c => c.Name))
                    {
                        select.AddOption(card, card);
                    }
                    select.MaxValues = select.Options.Count;

                    var builder = new ComponentBuilder().WithSelectMenu(select);

                    if (select.Options.Count > 0)
                    {
                        await arg.RespondAsync("Card to add", components: builder.Build(), ephemeral: true);
                    }
                    else
                    {
                        await arg.RespondAsync("You are already subscribed to all available cards.", ephemeral: true);
                    }

                    break;
                }
            case REMOVE_CARD_BUTTON:
                {
                    var currentSubscription = await _subscriptionManager.GetCurrentSubscription(arg.User.Id);
                    if (currentSubscription is null) return;

                    if (currentSubscription.CardNotifications.Count == 0)
                    {
                        await arg.RespondAsync("No card notifications to remove", ephemeral: true);
                        return;
                    }

                    var select = new SelectMenuBuilder()
                    {
                        Placeholder = "Select card to remove",
                        CustomId = REMOVE_CARD_DROPDOWN,
                        MaxValues = currentSubscription.CardNotifications.Count,
                        MinValues = 1,
                    };
                    foreach (var card in currentSubscription.CardNotifications.Select(n => n.CardName))
                    {
                        select.AddOption(card, card);
                    }

                    var builder = new ComponentBuilder().WithSelectMenu(select);

                    await arg.RespondAsync("Card to remove", components: builder.Build(), ephemeral: true);

                    break;
                }
            case UPDATE_VOTES_BUTTON:
                {
                    var textInput = new TextInputBuilder()
                    {
                        Label = "Number of votes required before alerting",
                        CustomId = UPDATE_VOTES_TEXTINPUT, 
                        MinLength = 1,
                        MaxLength = 3,
                        Required = true,
                    };

                    var modalBuilder = new ModalBuilder()
                    {
                        Title = "Vote update",
                        CustomId = UPDATE_VOTES_MODAL,
                    };
                    modalBuilder.AddTextInput(textInput);

                    await arg.RespondWithModalAsync(modalBuilder.Build());

                    break;
                }
            case REMOVE_ALL_NOTIFICATIONS_BUTTON:
                {
                    //Just clear server to avoid concurrency issues, will purge these records later
                    await _subscriptionManager.UpdateSubscriptionServer(arg.User.Id, string.Empty);

                    await BuildBasicCommandResponse(arg);

                    break;
                }
        }
    }

    public async Task SlashCommandExecuted(SocketSlashCommand arg)
    {
        if (arg.CommandName == MANAGE_NOTIFICATION_COMMAND)
        {
            await BuildBasicCommandResponse(arg);
        }
    }

    private async Task BuildBasicCommandResponse(IDiscordInteraction arg)
    {
        var subscription = await _subscriptionManager.GetCurrentSubscription(arg.User.Id);
        var text = BuildCurrentSubscriptionText(subscription);

        var noSubscription = string.IsNullOrWhiteSpace(subscription?.Server);

        var component = new ComponentBuilder();
        component.WithButton("Update Server", UPDATE_SERVER_BUTTON);
        component.WithButton("Add Card", ADD_CARD_BUTTON, ButtonStyle.Success, disabled: noSubscription);
        component.WithButton("Remove Card", REMOVE_CARD_BUTTON, ButtonStyle.Danger, disabled: string.IsNullOrWhiteSpace(subscription?.Server) || subscription.CardNotifications.Count == 0);
        component.WithButton("Update Minimum Votes", UPDATE_VOTES_BUTTON, disabled: noSubscription);
        component.WithButton("Remove All Notifications", REMOVE_ALL_NOTIFICATIONS_BUTTON, ButtonStyle.Danger, disabled: noSubscription);

        await arg.RespondAsync(text, components: component.Build(), ephemeral: true);
    }

    public async Task SelectMenuExecuted(SocketMessageComponent arg)
    {
        switch (arg.Data.CustomId)
        {
            case SELECT_REGION_DROPDOWN:
                {
                    var val = arg.Data.Values.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(val)) return;

                    var regions = await _dataController.GetServerRegions();

                    var select = new SelectMenuBuilder();
                    select.WithPlaceholder("Select server");
                    select.WithCustomId(SELECT_SERVER_DROPDOWN);
                    foreach (var server in regions[val].Servers)
                    {
                        select.AddOption(server, server);
                    }

                    var builder = new ComponentBuilder().WithSelectMenu(select);

                    await arg.RespondAsync("Server", components: builder.Build(), ephemeral: true);

                    break;
                }
            case SELECT_SERVER_DROPDOWN:
                {
                    var server = arg.Data.Values.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(server)) return;

                    var regions = await _dataController.GetServerRegions();
                    if (!regions.SelectMany(r => r.Value.Servers).Any(s => s == server)) return;

                    await _subscriptionManager.UpdateSubscriptionServer(arg.User.Id, server);

                    await BuildBasicCommandResponse(arg);

                    break;
                }
            case ADD_CARD_DROPDOWN:
                {
                    var cards = await _dataController.GetEpicLegendaryCards();
                    if(!arg.Data.Values.All(newCard => cards.Any(realCard => realCard.Name == newCard))) return;

                    await _subscriptionManager.AddCardsToSubscription(arg.User.Id, arg.Data.Values);

                    await BuildBasicCommandResponse(arg);

                    break;
                }
            case REMOVE_CARD_DROPDOWN:
                {
                    await _subscriptionManager.RemoveCardFromSubscription(arg.User.Id, arg.Data.Values);

                    await BuildBasicCommandResponse(arg);

                    break;
                }
        }
    }

    private static string BuildCurrentSubscriptionText(DiscordNotification? currentSubscription)
    {
        if(currentSubscription is null || string.IsNullOrWhiteSpace(currentSubscription.Server))
        {
            return "You have no active subscription data. Select a server to begin.";
        }

        string cardsText;
        if(currentSubscription.CardNotifications.Count > 0)
        {
            cardsText = string.Join(", ", currentSubscription.CardNotifications.Select(n => n.CardName));
        }
        else
        {
            cardsText = "No cards selected. Select cards below.";
        }

        return @$"
Subscribed Server: {Format.Bold(currentSubscription.Server)}
Selected Cards: {cardsText}
Minimum votes to trigger alert: {currentSubscription.CardVoteThreshold}
";
    }
}
