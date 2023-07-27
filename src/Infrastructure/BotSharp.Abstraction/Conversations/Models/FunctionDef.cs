using System.Text.Json;

namespace BotSharp.Abstraction.Conversations.Models;

public class FunctionDef
{
    public string Name { get; set; }
    public string Description { get; set; }
    public JsonDocument Parameters { get; set; }

    public override string ToString()
    {
        return $"{Name}: {Description}";
    }
}
