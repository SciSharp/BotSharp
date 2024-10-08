using System.ComponentModel.DataAnnotations.Schema;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class LlmCompletionLog
{
    public string Id { get; set; }
    public string ConversationId { get; set; }

    [Column(TypeName = "json")]
    public List<PromptLog> Logs { get; set; }
}
