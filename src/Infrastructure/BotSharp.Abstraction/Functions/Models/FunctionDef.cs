namespace BotSharp.Abstraction.Functions.Models;

public class FunctionDef
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string? Impact { get; set; }
    public FunctionParametersDef Parameters { get; set; } = new FunctionParametersDef();

    public override string ToString()
    {
        return $"{Name}: {Description}";
    }
}
