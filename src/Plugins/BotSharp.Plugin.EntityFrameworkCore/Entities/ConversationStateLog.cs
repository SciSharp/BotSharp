using System.ComponentModel.DataAnnotations.Schema;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class ConversationStateLog
{
    public string Id { get; set; }
    public string ConversationId { get; set; }
    public string MessageId { get; set; }

    [Column(TypeName = "json")]
    public Dictionary<string, string> States { get; set; }
    public DateTime CreatedTime { get; set; }
}
