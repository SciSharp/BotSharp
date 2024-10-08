using System.Collections.Generic;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class FunctionDef
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string>? Channels { get; set; }
    public string? VisibilityExpression { get; set; }
    public string? Impact { get; set; }
    public FunctionParametersDef Parameters { get; set; } = new FunctionParametersDef();
}
