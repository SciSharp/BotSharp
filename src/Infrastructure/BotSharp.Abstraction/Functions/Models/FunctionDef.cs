namespace BotSharp.Abstraction.Functions.Models;

public class FunctionDef
{
    public string Name { get; set; }
    public string Description { get; set; }
    public FunctionParametersDef Parameters { get; set; }

    public override string ToString()
    {
        return $"{Name}: {Description}";
    }
}
