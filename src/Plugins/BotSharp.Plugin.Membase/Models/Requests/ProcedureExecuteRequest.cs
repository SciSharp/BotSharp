namespace BotSharp.Plugin.Membase.Models;

public class ProcedureExecuteRequest
{
    public Dictionary<string, object?> Parameters { get; set; } = [];
}
