using BotSharp.Abstraction.Repositories.Models;
using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Repositories.Records;

public class ConversationRecord : RecordBase
{
    [Required]
    [MaxLength(36)]
    public Guid AgentId { get; set; } = Guid.Empty;

    [Required]
    [MaxLength(36)]
    public Guid UserId { get; set; } = Guid.Empty;

    [MaxLength(64)]
    public string Title { get; set; } = string.Empty;

    [JsonIgnore]
    public string Dialog { get; set; }

    [JsonIgnore]
    public List<KeyValueModel> States { get; set; }

    [Required]
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
