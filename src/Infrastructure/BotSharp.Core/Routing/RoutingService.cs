using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Core.Routing;

public class RoutingService : IRoutingService
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private List<RoleDialogModel> _dialogs;
    public List<RoleDialogModel> Dialogs => _dialogs;

    public RoutingService(IServiceProvider services, ILogger<RoutingService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<RoleDialogModel> Enter(Agent agent, List<RoleDialogModel> whileDialogs)
    {
        _dialogs = new List<RoleDialogModel>();
        RoleDialogModel result = new RoleDialogModel(AgentRole.Assistant, "not handled");

        foreach (var dialog in whileDialogs.TakeLast(10))
        {
            agent.Instruction += $"\r\n{dialog.Role}: {dialog.Content}";
        }

        var inst = await GetNextInstructionFromReasoner(agent);
        int loopCount = 0;
        while (loopCount < 3)
        {
            loopCount++;
            if (inst.Function == "continue_execute_task")
            {
                var router = _services.GetRequiredService<IAgentRouting>();
                var db = _services.GetRequiredService<IBotSharpRepository>();
                var record = db.Agents.First(x => x.Name.ToLower() == inst.Parameters.AgentName.ToLower());

                result = new RoleDialogModel(AgentRole.Function, inst.Parameters.Question)
                {
                    FunctionName = inst.Function,
                    FunctionArgs = JsonSerializer.Serialize(inst.Parameters.Arguments),
                    CurrentAgentId = record.Id,
                };
                break;
            }
            // Compatible with previous Router, can be removed in the future.
            else if (inst.Function == "route_to_agent")
            {
                var function = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name == inst.Function);
                result = new RoleDialogModel(AgentRole.Function, inst.Parameters.Question)
                {
                    FunctionName = inst.Function,
                    FunctionArgs = JsonSerializer.Serialize(new RoutingArgs
                    {
                        AgentName = inst.Parameters.AgentName
                    }),
                };
                var ret = await function.Execute(result);
                break;
            }
            else if (inst.Function == "interrupt_task_execution")
            {
                result = new RoleDialogModel(AgentRole.User, inst.Parameters.Reason)
                {
                    FunctionName = inst.Function
                };
                break;
            }
            else if (inst.Function == "response_to_user")
            {
                result = new RoleDialogModel(AgentRole.User, inst.Parameters.Answer)
                {
                    FunctionName = inst.Function
                };
                break;
            }
            else if (inst.Function == "retrieve_data_from_agent")
            {
                // Retrieve information from specific agent
                var db = _services.GetRequiredService<IBotSharpRepository>();
                var record = db.Agents.First(x => x.Name.ToLower() == inst.Parameters.AgentName.ToLower());
                var response = await RetrieveDataFromAgent(record.Id, new List<RoleDialogModel>
                {
                    new RoleDialogModel(AgentRole.User, inst.Parameters.Question)
                });

                response.Content += $"\r\nDo you want to continue current task?"; 
                _dialogs.Add(new RoleDialogModel(AgentRole.Function, $"{record.Name}: {response.Content}")
                {
                    FunctionName = inst.Function,
                    FunctionArgs = JsonSerializer.Serialize(inst.Parameters.Arguments),
                    ExecutionResult = response.Content,
                    CurrentAgentId = record.Id
                });

                agent.Instruction += $"\r\n{record.Name}: {response.Content}";

                // Got the response from agent, then send to reasoner again to make the decision
                inst = await GetNextInstructionFromReasoner(agent);
            }
        }

        return result;
    }

    private async Task<FunctionCallFromLlm> GetNextInstructionFromReasoner(Agent reasoner)
    {
        var wholeDialogs = new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, @"What's the next step? Response in JSON format with ""function"" and ""parameters"".")
        };

        var chatCompletion = CompletionProvider.GetChatCompletion(_services);

        RoleDialogModel response = null;
        await chatCompletion.GetChatCompletionsAsync(reasoner, wholeDialogs, async msg
            => response = msg, fn
            => Task.CompletedTask);

        var args = JsonSerializer.Deserialize<FunctionCallFromLlm>(response.Content);

        if (args.Parameters.Arguments != null)
        {
            SaveStateByArgs(args.Parameters.Arguments);
        }

        args.Function = args.Function.Split('.').Last();

        return args;
    }

    private async Task<RoleDialogModel> RetrieveDataFromAgent(string agentId, List<RoleDialogModel> wholeDialogs)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        var chatCompletion = CompletionProvider.GetChatCompletion(_services);

        RoleDialogModel response = null;
        await chatCompletion.GetChatCompletionsAsync(agent, wholeDialogs, async msg
            => response = msg, async fn
            =>
            {
                // execute function
                // Save states
                SaveStateByArgs(JsonSerializer.Deserialize<JsonDocument>(fn.FunctionArgs));

                var conversationService = _services.GetRequiredService<IConversationService>();
                // Call functions
                await conversationService.CallFunctions(fn);

                response = fn;
                response.Content = fn.ExecutionResult;
            });
        return response;
    }

    private void SaveStateByArgs(JsonDocument args)
    {
        if (args == null)
        {
            return;
        }

        var stateService = _services.GetRequiredService<IConversationStateService>();
        if (args.RootElement is JsonElement root)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!string.IsNullOrEmpty(property.Value.ToString()))
                {
                    stateService.SetState(property.Name, property.Value);
                }
            }
        }
    }
}
