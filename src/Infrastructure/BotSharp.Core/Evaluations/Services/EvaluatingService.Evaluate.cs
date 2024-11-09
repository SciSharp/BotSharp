using BotSharp.Abstraction.Evaluations.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;

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

        var generatedConvId = await SimulateConversation(initMessage, refDialogContents, request);

        return new EvaluationResult
        {
            GeneratedConversationId = generatedConvId
        };
    }

    private async Task<string> SimulateConversation(string initMessage, IEnumerable<string> refDialogs, EvaluationRequest request)
    {
        var count = 0;
        var duplicateCount = 0;
        var convId = Guid.NewGuid().ToString();
        var curDialogs = new List<string>();
        var curUserMsg = initMessage;
        var prevUserMsg = string.Empty;
        var curBotMsg = string.Empty;
        var prevBotMsg = string.Empty;

        var storage = _services.GetRequiredService<IConversationStorage>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var instructService = _services.GetRequiredService<IInstructService>();

        var query = "Please see yourself as a user and follow the instruction to generate a message.";
        var targetAgentId = request.AgentId;
        var evaluatorAgent = await agentService.GetAgent(BuiltInAgentId.Evaluator);
        var simulatorPrompt = evaluatorAgent.Templates.FirstOrDefault(x => x.Name == "instruction.simulator")?.Content ?? string.Empty;

        while (true)
        {
            curDialogs.Add($"{AgentRole.User}: {curUserMsg}");
            var dialog = await SendMessage(targetAgentId, convId, curUserMsg);

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
                                    { "additional_instruction", request.AdditionalInstruction },
                                    { "stop_criteria", request.StopCriteria }
                                }
                            });

            _logger.LogInformation($"Generated message: {result?.GeneratedMessage}, stop: {result?.Stop}, reason: {result?.Reason}");

            if (count > request.MaxRounds || (result != null && result.Stop))
            {
                break;
            }


            if (curUserMsg.IsEqualTo(prevUserMsg) || curBotMsg.IsEqualTo(prevBotMsg))
            {
                duplicateCount++;
            }
            else
            {
                duplicateCount = 0;
            }


            if (duplicateCount >= request.DuplicateLimit)
            {
                break;
            }

            prevUserMsg = curUserMsg;
            curUserMsg = result?.GeneratedMessage ?? string.Empty;
        }

        return convId;
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
}
