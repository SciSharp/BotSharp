using BotSharp.Abstraction.Utilities;

namespace BotSharp.Abstraction.Repositories.Records;

public class AgentRecord : RecordBase
{
    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public string Instruction { get; set; }

    public List<string> Functions { get; set; }

    public List<string> Responses { get; set; }

    public string Samples { get; set; }
    public bool IsPublic { get; set; }

    [Required]
    public DateTime CreatedTime { get; set; }

    [Required]
    public DateTime UpdatedTime { get; set; }
}
