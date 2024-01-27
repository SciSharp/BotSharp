using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using BotSharp.Plugin.RoutingSpeeder.Settings;
using BotSharp.Abstraction.Templating;
using BotSharp.Plugin.RoutingSpeeder.Providers;
using BotSharp.Abstraction.Agents;
using System.IO;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Abstraction.Routing.Models;

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
    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        var intentClassifier = _services.GetRequiredService<IntentClassifier>();
        var vector = intentClassifier.GetTextEmbedding(message.Content);

        // intentClassifier.Train();
        // Utilize local discriminative model to predict intent
        var context = _services.GetRequiredService<RoutingContext>();
        context.IntentName = intentClassifier.Predict(vector);

        if (string.IsNullOrEmpty(context.IntentName))
        {
            return;
        }

        // Render by template
        var templateService = _services.GetRequiredService<IResponseTemplateService>();
        var response = await templateService.RenderIntentResponse(_agent.Id, message);

        if (!string.IsNullOrEmpty(response))
        {
            message.Content = response;
            message.StopCompletion = true;
        }
    }

    public override async Task OnResponseGenerated(RoleDialogModel message)
    {
        var routerSettings = _services.GetRequiredService<RoutingSettings>();
        bool saveFlag = _agent.Type != AgentType.Routing;

        if (saveFlag)
        {
            // save train data
            var agentService = _services.CreateScope().ServiceProvider.GetRequiredService<IAgentService>();
            var rootDataPath = agentService.GetDataDir();

            string rawDataDir = Path.Combine(rootDataPath, "raw_data", $"agent.{message.CurrentAgentId}.txt");
            var lastThreeDialogs = _dialogs.Where(x => x.Role == AgentRole.User || x.Role == AgentRole.Assistant)
                .Select(x => x.Content.Replace('\r', ' ').Replace('\n', ' '))
                .TakeLast(3)
                .ToArray();

            var content = string.Join(' ', lastThreeDialogs) + Environment.NewLine;
            if (!File.Exists(rawDataDir))
            {
                await File.WriteAllTextAsync(rawDataDir, content);
            }
            else
            {
                await File.AppendAllTextAsync(rawDataDir, content);
            }
        }
    }
}
