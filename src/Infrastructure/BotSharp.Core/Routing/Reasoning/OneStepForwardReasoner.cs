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

using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Reasoning;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing.Reasoning;

/// <summary>
/// One-step forward reasoning is a straightforward reasoning approach where the model or agent evaluates its current state 
/// and takes the next best logical step toward the solution without extensive lookahead or planning. 
/// This type of reasoning involves making a decision based on the current situation and immediate context 
/// rather than considering multiple future steps or possibilities.
/// </summary>
public class OneStepForwardReasoner : IRoutingReasoner
{
    public string Name => "one-step-forward";

    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public OneStepForwardReasoner(IServiceProvider services, ILogger<OneStepForwardReasoner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs)
    {
        var next = GetNextStepPrompt(router);

        var inst = new FunctionCallFromLlm();

        // chat completion
        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: router?.LlmConfig?.Provider,
            model: router?.LlmConfig?.Model);

        int retryCount = 0;
        while (retryCount < 3)
        {
            string text = string.Empty;
            try
            {
                // text completion
                // text = await completion.GetCompletion(content, router.Id, messageId);
                dialogs = new List<RoleDialogModel>
                {
                    new RoleDialogModel(AgentRole.User, next)
                    {
                        FunctionName = Name,
                        MessageId = messageId
                    }
                };
                var response = await completion.GetChatCompletions(router, dialogs);

                inst = response.Content.JsonContent<FunctionCallFromLlm>();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}: {text}");
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
        // Set user content as Planner's question
        message.FunctionName = inst.Function;
        message.FunctionArgs = inst.Arguments == null ? "{}" : JsonSerializer.Serialize(inst.Arguments);

        return true;
    }

    public async Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var context = _services.GetRequiredService<IRoutingContext>();
        if (inst.UnmatchedAgent)
        {
            var unmatchedAgentId = context.GetCurrentAgentId();

            // Exclude the wrong routed agent
            var agents = router.TemplateDict["routing_agents"] as RoutableAgent[];
            router.TemplateDict["routing_agents"] = agents.Where(x => x.AgentId != unmatchedAgentId).ToArray();

            // Handover to Router;
            context.Pop();
        }
        else
        {
            context.Empty(reason: $"Agent queue is cleared by {nameof(OneStepForwardReasoner)}");
            // context.Push(inst.OriginalAgent, "Push user goal agent");
        }
        return true;
    }

    private string GetNextStepPrompt(Agent router)
    {
        var template = router.Templates.First(x => x.Name == "reasoner.one-step-forward").Content;

        var states = _services.GetRequiredService<IConversationStateService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { StateConst.EXPECTED_ACTION_AGENT,  states.GetState(StateConst.EXPECTED_ACTION_AGENT) },
            { StateConst.EXPECTED_GOAL_AGENT,  states.GetState(StateConst.EXPECTED_GOAL_AGENT) }
        });
    }
}
