/*****************************************************************************
  Copyright 2024 Written by Haiping Chen. All Rights Reserved.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
******************************************************************************/

using BotSharp.Abstraction.Routing.Reasoning;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing.Reasoning;

/// <summary>
/// Human feedback based reasoner
/// </summary>
public class HFReasoner : IRoutingReasoner
{
    public string Name => "Human-Feedback Reasoner";
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public HFReasoner(IServiceProvider services, ILogger<HFReasoner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs)
    {
        var next = GetNextStepPrompt(router);

        RoleDialogModel response = default;
        var inst = new FunctionCallFromLlm();

        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: router?.LlmConfig?.Provider,
            model: router?.LlmConfig?.Model);

        int retryCount = 0;
        while (retryCount < 3)
        {
            try
            {
                dialogs = new List<RoleDialogModel>
                {
                    new RoleDialogModel(AgentRole.User, next)
                    {
                        FunctionName = nameof(HFReasoner),
                        MessageId = messageId
                    }
                };
                response = await completion.GetChatCompletions(router, dialogs);

                inst = response.Content.JsonContent<FunctionCallFromLlm>();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}: {response.Content}");
                inst.Function = "response_to_user";
                inst.Response = ex.Message;
                inst.AgentName = "Router";
            }
            finally
            {
                retryCount++;
            }
        }

        // Fix LLM malformed response
        ReasonerHelper.FixMalformedResponse(_services, inst);

        return inst;
    }

    public async Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        if (!string.IsNullOrEmpty(inst.AgentName))
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var filter = new AgentFilter { AgentName = inst.AgentName };
            var agent = db.GetAgents(filter).FirstOrDefault();

            var context = _services.GetRequiredService<IRoutingContext>();
            context.Push(agent.Id, reason: inst.NextActionReason);

            // Set user content as Planner's question
            message.FunctionName = inst.Function;
            message.FunctionArgs = inst.Arguments == null ? "{}" : JsonSerializer.Serialize(inst.Arguments);
        }

        return true;
    }

    public async Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var context = _services.GetRequiredService<IRoutingContext>();
        context.Empty(reason: $"Agent queue is cleared by {nameof(HFReasoner)}");
        return true;
    }

    private string GetNextStepPrompt(Agent router)
    {
        var template = router.Templates.First(x => x.Name == "reasoner.hf").Content;
        var render = _services.GetRequiredService<ITemplateRender>();
        // update states
        var conv = _services.GetRequiredService<IConversationService>();
        foreach (var t in conv.States.GetStates())
        {
            router.TemplateDict[t.Key] = t.Value;
        }
        var prompt = render.Render(template, router.TemplateDict);
        return prompt.Trim();
    }
}
