using BotSharp.Abstraction.Evaluations.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Core.Evaluations.Services;

public partial class EvaluatingService
{
    public async Task<EvaluationResult> Evaluate(string conversationId, EvaluationRequest request)
    {
        var storage = _services.GetRequiredService<IConversationStorage>();
        var refDialogs = storage.GetDialogs(request.RefConversationId);
        var refDialogContents = GetConversationContent(refDialogs);

        var initDialog = refDialogs.FirstOrDefault(x => x.Role == AgentRole.User);
        var initMessage = initDialog?.RichContent?.Message?.Text ?? initDialog?.Content ?? "Hello";

        var generatedConvId = await SimulateConversation(initMessage, refDialogContents, request);

        return new EvaluationResult
        {
            GeneratedConversationId = generatedConvId
        };
    }

    private async Task<string> SimulateConversation(string initMessage, IEnumerable<string> refConversation, EvaluationRequest request)
    {
        var count = 0;
        var curConvId = Guid.NewGuid().ToString();
        var curConversation = new List<string>();
        var curMessage = initMessage;

        var storage = _services.GetRequiredService<IConversationStorage>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var instructService = _services.GetRequiredService<IInstructService>();

        var query = "Please take yourself as a user and follow the instruction to generate a message in the user tone.";
        var targetAgentId = request.AgentId;
        var evaluatorAgent = await agentService.GetAgent(BuiltInAgentId.Evaluator);
        var simulatorPrompt = evaluatorAgent.Templates.FirstOrDefault(x => x.Name == "instruction.simulator")?.Content ?? string.Empty;

        while (true)
        {
            curConversation.Add($"{AgentRole.User}: {curMessage}");
            var dialog = await SendMessage(targetAgentId, curConvId, curMessage);
            var botMessage = dialog?.RichContent?.Message?.Text ?? dialog?.Content ?? string.Empty;
            curConversation.Add($"{AgentRole.Assistant}: {botMessage}");
            count++;

            var result = await instructService.Instruct<SimulationResult>(simulatorPrompt, BuiltInAgentId.Evaluator,
                            new InstructOptions
                            {
                                Provider = request.Provider,
                                Model = request.Model,
                                Message = query,
                                Data = new Dictionary<string, object>
                                {
                                    { "ref_conversation", refConversation },
                                    { "cur_conversation", curConversation },
                                }
                            });

            if (count > request.MaxRounds || (result != null && result.Stop))
            {
                break;
            }

            curMessage = result?.GeneratedMessage ?? string.Empty;
        }

        return curConvId;
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

            contents.Add($"{role}: {dialog.RichContent?.Message?.Text ?? dialog.Content}");
        }

        return contents;
    }
}
