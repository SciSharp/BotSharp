using BotSharp.Abstraction.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class ChatHubConversationHook : ConversationHookBase
{
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;

    public ChatHubConversationHook(IServiceProvider services,
        IHubContext<SignalRHub> chatHub)
    {
        _services = services;
        _chatHub = chatHub;
    }

    public override async Task OnUserAgentConnectedInitially(Conversation conversation)
    {
        var agentService = _services.GetService<IAgentService>();
        var agent = await agentService.LoadAgent(conversation.AgentId);

        // Check if the Welcome template exists.
        var welcomeTemplate = agent.Templates.FirstOrDefault(x => x.Name == "welcome");
        if (welcomeTemplate != null)
        {
            var messages = JsonSerializer.Deserialize<RichMessageBase[]>(welcomeTemplate.Content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            foreach (var message in messages)
            {
                await Task.Delay(300);
                await OnResponseGenerated(new RoleDialogModel(AgentRole.Assistant, message.Text));
            }
        }

        await base.OnUserAgentConnectedInitially(conversation);
    }

    public override async Task OnConversationInitialized(Conversation conversation)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var conv = ConversationViewModel.FromSession(conversation);

        var user = await userService.GetUser(conv.User.Id);
        conv.User = UserViewModel.FromUser(user);

        await _chatHub.Clients.All.SendAsync("OnConversationInitFromClient", conv);

        await base.OnConversationInitialized(conversation);
    }

    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var userService = _services.GetRequiredService<IUserService>();
        var sender = await userService.GetMyProfile();

        // Update console conversation UI for CSR
        await _chatHub.Clients.All.SendAsync("OnMessageReceivedFromClient", new ChatResponseModel()
        {
            ConversationId = conv.ConversationId,
            MessageId = message.MessageId,
            Text = message.Content,
            Sender = UserViewModel.FromUser(sender)
        });

        await base.OnMessageReceived(message);
    }

    public override async Task OnResponseGenerated(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();

        await _chatHub.Clients.All.SendAsync("OnMessageReceivedFromAssistant", new ChatResponseModel()
        {
            ConversationId = conv.ConversationId,
            MessageId = message.MessageId,
            Text = message.Content,
            Sender = new UserViewModel()
            {
                FirstName = "AI",
                LastName = "Assistant",
                Role = AgentRole.Assistant
            }
        });

        await base.OnResponseGenerated(message);
    }
}
