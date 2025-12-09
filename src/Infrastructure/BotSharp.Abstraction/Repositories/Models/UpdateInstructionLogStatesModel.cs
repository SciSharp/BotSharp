namespace BotSharp.Abstraction.Repositories.Models;

public class UpdateInstructionLogStatesModel
{
    public string LogId { get; set; } = default!;
    public string StateKeyPrefix { get; set; } = "new_";
    public Dictionary<string, string> States { get; set; } = [];
}
