using BotSharp.Abstraction.Evaluations.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Models;

namespace BotSharp.Core.Evaluations.Services;

public partial class EvaluatingService
{
    public async Task<EvaluationResult> Evaluate(string conversationId, EvaluationRequest request)
    {
        var result = new EvaluationResult();
        if (string.IsNullOrEmpty(conversationId))
        {
            return result;
        }

        var storage = _services.GetRequiredService<IConversationStorage>();
        var refDialogs = storage.GetDialogs(conversationId);

        if (refDialogs.IsNullOrEmpty())
        {
            return result;
        }

        var refDialogContents = GetConversationContent(refDialogs);
        var initDialog = refDialogs.FirstOrDefault(x => x.Role == AgentRole.User);
        var initMessage = initDialog?.RichContent?.Message?.Text ?? initDialog?.Content;

        if (string.IsNullOrWhiteSpace(initMessage))
        {
            return result;
        }

        var initialStates = GetInitialStates(conversationId);
        var generatedConvId = await SimulateConversation(initMessage, refDialogContents, request, initialStates);
        var metricResult = await EvaluateMetrics(generatedConvId, refDialogContents, request);

        return new EvaluationResult
        {
            GeneratedConversationId = generatedConvId,
            MetricResult = metricResult
        };
    }

    private async Task<string> SimulateConversation(string initMessage, IEnumerable<string> refDialogs,
        EvaluationRequest request, IEnumerable<MessageState>? states = null)
    {
        var count = 0;
        var duplicateCount = 0;
        var convId = Guid.NewGuid().ToString();
        var curDialogs = new List<string>();
        var curUserMsg = initMessage;
        var prevUserMsg = string.Empty;
        var curBotMsg = string.Empty;
        var prevBotMsg = string.Empty;
        var initialStates = states?.ToList() ?? [];

        var storage = _services.GetRequiredService<IConversationStorage>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var instructService = _services.GetRequiredService<IInstructService>();

        var query = "Please see yourself as a user and follow the instruction to generate a message.";
        var targetAgentId = request.AgentId;
        var evaluator = await agentService.GetAgent(BuiltInAgentId.Evaluator);
        var simulatorPrompt = evaluator.Templates.FirstOrDefault(x => x.Name == "instruction.simulator")?.Content ?? string.Empty;

        while (true)
        {
            curDialogs.Add($"{AgentRole.User}: {curUserMsg}");
            var dialog = await SendMessage(targetAgentId, convId, curUserMsg, states: initialStates);
            initialStates = [];

            prevBotMsg = curBotMsg;
            curBotMsg = dialog?.RichContent?.Message?.Text ?? dialog?.Content ?? string.Empty;
            curDialogs.Add($"{AgentRole.Assistant}: {curBotMsg}");

            count++;

            var result = await instructService.Instruct<SimulationResult>(simulatorPrompt, BuiltInAgentId.Evaluator,
                            new InstructOptions
                            {
                                Provider = request.Provider,
                                Model = request.Model,
                                Message = query,
                                Data = new Dictionary<string, object>
                                {
                                    { "ref_conversation", refDialogs },
                                    { "cur_conversation", curDialogs },
                                    { "additional_instruction", request.Chat.AdditionalInstruction },
                                    { "stop_criteria", request.Chat.StopCriteria }
                                }
                            });

            _logger.LogInformation($"Generated message: {result?.GeneratedMessage}, stop: {result?.Stop}, reason: {result?.Reason}");

            if (count > request.Chat.MaxRounds || (result != null && result.Stop))
            {
                break;
            }

            duplicateCount = curBotMsg.IsEqualTo(prevBotMsg) ? duplicateCount + 1 : 0;
            if (duplicateCount >= request.Chat.DuplicateLimit)
            {
                break;
            }

            prevUserMsg = curUserMsg;
            curUserMsg = result?.GeneratedMessage ?? string.Empty;
        }

        return convId;
    }


    private async Task<string?> EvaluateMetrics(string curConversationId, IEnumerable<string> refDialogs, EvaluationRequest request)
    {
        var storage = _services.GetRequiredService<IConversationStorage>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var instructService = _services.GetRequiredService<IInstructService>();

        var curDialogs = storage.GetDialogs(curConversationId);
        var curDialogContents = GetConversationContent(curDialogs);

        var evaluator = await agentService.GetAgent(BuiltInAgentId.Evaluator);
        var metricPrompt = evaluator.Templates.FirstOrDefault(x => x.Name == "instruction.metrics")?.Content ?? string.Empty;
        var query = "Please follow the instruction for evaluation.";

        var result = await instructService.Instruct<JsonDocument>(metricPrompt, BuiltInAgentId.Evaluator,
                            new InstructOptions
                            {
                                Provider = request.Provider,
                                Model = request.Model,
                                Message = query,
                                Data = new Dictionary<string, object>
                                {
                                    { "ref_conversation", refDialogs },
                                    { "cur_conversation", curDialogs },
                                    { "additional_instruction", request.Metric.AdditionalInstruction },
                                    { "metrics", request.Metric.Metrics }
                                }
                            });

        return result != null ? result.RootElement.GetRawText() : null;
    }

    private IEnumerable<string> GetConversationContent(IEnumerable<RoleDialogModel> dialogs)
    {
        var contents = new List<string>();

        foreach (var dialog in dialogs)
        {
            var role = dialog.Role;
            if (role == AgentRole.Function) continue;

            if (role != AgentRole.User)
            {
                role = AgentRole.Assistant;
            }

            contents.Add($"{role}: {dialog.RichContent?.Message?.Text ?? dialog.Content ?? string.Empty}");
        }

        return contents;
    }

    private IEnumerable<MessageState> GetInitialStates(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return [];
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var states = db.GetConversationStates(conversationId);
        var initialStates = new List<MessageState>();

        foreach (var state in states)
        {
            var value = state.Value?.Values?.FirstOrDefault(x => string.IsNullOrEmpty(x.MessageId));

            if (string.IsNullOrEmpty(value?.Data))
            {
                continue;
            }

            initialStates.Add(new MessageState(state.Key, value.Data, value.ActiveRounds));
        }

        return initialStates;
    }
}
