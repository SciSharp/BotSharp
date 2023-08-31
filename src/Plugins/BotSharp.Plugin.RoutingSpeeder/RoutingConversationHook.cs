using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using FastText.NetWrapper;
using BotSharp.Plugin.RoutingSpeeder.Settings;

namespace BotSharp.Plugin.RoutingSpeeder;

public class RoutingConversationHook: ConversationHookBase
{
    private readonly IServiceProvider _services;
    private routerSpeedSettings _settings;
    public RoutingConversationHook(IServiceProvider service, routerSpeedSettings settings)
    {
        _services = service;
        _settings = settings;
    }
    public override async Task BeforeCompletion(RoleDialogModel message)
    {
        var embedding = _services.GetServices<ITextEmbedding>()
            .FirstOrDefault(x => x.GetType().FullName.EndsWith(_settings.TextEmbedding));

        // Utilize local discriminative model to predict intent
        message.Content = "response content";
        message.StopCompletion = true;
    }
}
