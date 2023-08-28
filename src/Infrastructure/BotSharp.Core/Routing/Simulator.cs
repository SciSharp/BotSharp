using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Routing;

/// <summary>
/// Simulate the dialogue between different agents.
/// </summary>
public class Simulator
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private List<RoleDialogModel> _dialogs;
    public List<RoleDialogModel> Dialogs => _dialogs;

    public Simulator(IServiceProvider services, ILogger<Simulator> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<RoleDialogModel> Enter(Agent agent, List<RoleDialogModel> whileDialogs)
    {
        _dialogs = new List<RoleDialogModel>();

        foreach (var dialog in whileDialogs.TakeLast(10))
        {
            agent.Instruction += $"\r\n{dialog.Role}: {dialog.Content}";
        }
        
        var response = await SendMessageToReasoner(agent);
        var args = JsonSerializer.Deserialize<FunctionCallFromLlm>(response.Content);
        response.FunctionName = args.Function;
        
        if (args.Function == "continue_execute_task")
        {
            response.FunctionArgs = JsonSerializer.Serialize(args.Parameters.Arguments);

            var router = _services.GetRequiredService<IAgentRouting>();
            var record = router.GetRecordByName(args.Parameters.AgentName);
            response.CurrentAgentId = record.AgentId;
        }
        else if (args.Function == "interrupt_task_execution")
        {
            response.Content = args.Parameters.Reason;
            response.ExecutionResult = args.Parameters.Reason;
        }
        else if (args.Function == "response_to_user")
        {
            response.Content = args.Parameters.Answer;
            response.ExecutionResult = args.Parameters.Answer;
        }

        return response;
    }

    private async Task<RoleDialogModel> SendMessageToReasoner(Agent reasoner)
    {
        var wholeDialogs = new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, @"What's the next step,  your response must be in JSON format with ""function"" and ""parameters"". ")
        };

        var chatCompletion = GetGpt4ChatCompletion();

        RoleDialogModel response = null;
        await chatCompletion.GetChatCompletionsAsync(reasoner, wholeDialogs, async msg
            => response = msg, fn 
            => Task.CompletedTask);

        var args = JsonSerializer.Deserialize<FunctionCallFromLlm>(response.Content);

        if (args.Function == "retrieve_data_from_agent")
        {
            SaveStateByArgs(args.Parameters.Arguments);
        }
        else if (args.Function == "response_to_user")
        {
            return response;
        }

        // Retrieve information from specific agent
        var router = _services.GetRequiredService<IAgentRouting>();
        var record = router.GetRecordByName(args.Parameters.AgentName);
        response = await SendMessageToAgent(record.AgentId, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, args.Parameters.Question)
        });

        _dialogs.Add(new RoleDialogModel(AgentRole.Function, $"{record.Name}: {response.Content}")
        {
            FunctionName = args.Function,
            FunctionArgs = JsonSerializer.Serialize(args.Parameters.Arguments),
            ExecutionResult = response.Content
        });

        reasoner.Instruction += $"\r\n{record.Name}: {response.Content}";
        // Got the response from agent, then send to reasoner again to make the decision
        await chatCompletion.GetChatCompletionsAsync(reasoner, wholeDialogs, async msg
            => response = msg, fn
            => Task.CompletedTask);

        return response;
    }

    private async Task<RoleDialogModel> SendMessageToAgent(string agentId, List<RoleDialogModel> wholeDialogs)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        var chatCompletion = GetChatCompletion();

        RoleDialogModel response = null;
        await chatCompletion.GetChatCompletionsAsync(agent, wholeDialogs, async msg
            => response = msg, fn
            => Task.CompletedTask);
        return response;
    }

    public IChatCompletion GetChatCompletion()
    {
        var completions = _services.GetServices<IChatCompletion>();
        var settings = _services.GetRequiredService<ConversationSetting>();
        return completions.FirstOrDefault(x => x.GetType().FullName.EndsWith(settings.ChatCompletion));
    }

    public IChatCompletion GetGpt4ChatCompletion()
    {
        var completions = _services.GetServices<IChatCompletion>();
        return completions.FirstOrDefault(x => x.GetType().FullName.EndsWith("GPT4CompletionProvider"));
    }

    private void SaveStateByArgs(JsonDocument args)
    {
        var stateService = _services.GetRequiredService<IConversationStateService>();
        if (args.RootElement is JsonElement root)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!string.IsNullOrEmpty(property.Value.ToString()))
                {
                    stateService.SetState(property.Name, property.Value.ToString());
                }
            }
        }
    }
}
