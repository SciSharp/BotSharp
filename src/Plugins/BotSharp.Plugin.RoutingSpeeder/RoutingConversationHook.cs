using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Templating;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BotSharp.Plugin.RoutingSpeeder;

public class RoutingConversationHook: ConversationHookBase
{
    private readonly IServiceProvider _services;
    public RoutingConversationHook(IServiceProvider services)
    {
        _services = services;
    }

    public override async Task BeforeCompletion(RoleDialogModel message)
    {
        // Utilize local discriminative model to predict intent
        message.IntentName = "greeting";

        // Render by template
        var templateService = _services.GetRequiredService<IResponseTemplateService>();
        var response = await templateService.RenderIntentResponse(_agent.Id, message);

        if (!string.IsNullOrEmpty(response))
        {
            message.Content = response;
            message.StopCompletion = true;
        }
    }
}
