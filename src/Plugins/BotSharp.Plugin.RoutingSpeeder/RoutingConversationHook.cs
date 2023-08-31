using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using BotSharp.Plugin.RoutingSpeeder.Settings;
using BotSharp.Abstraction.Templating;
using BotSharp.Plugin.RoutingSpeeder.Providers;

namespace BotSharp.Plugin.RoutingSpeeder;

public class RoutingConversationHook: ConversationHookBase
{
    private readonly IServiceProvider _services;
    private RouterSpeederSettings _settings;
    public RoutingConversationHook(IServiceProvider service, RouterSpeederSettings settings)
    {
        _services = service;
        _settings = settings;
    }
    public override async Task BeforeCompletion(RoleDialogModel message)
    {
        var intentClassifier = _services.GetRequiredService<IntentClassifier>();
        var vector = intentClassifier.GetTextEmbedding(message.Content);

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
