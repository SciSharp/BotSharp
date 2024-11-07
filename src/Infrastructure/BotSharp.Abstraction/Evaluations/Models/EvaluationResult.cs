namespace BotSharp.Abstraction.Evaluations.Models;

public class EvaluationResult
{
    public List<RoleDialogModel> Dialogs { get; set; }
    public string TaskInstruction { get; set; }
    public string SystemPrompt { get; set; }
    public string GeneratedConversationId { get; set; }
}
