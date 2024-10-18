using System.ComponentModel.DataAnnotations.Schema;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class ExecutionLog
{
    public string Id { get; set; }
    public string ConversationId { get; set; }

    [Column(TypeName = "json")]
    public List<string> Logs { get; set; }
}
