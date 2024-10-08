using System.Collections.Generic;

namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class FunctionParametersDef
{
    public string Type { get; set; }
    public string Properties { get; set; }
    public List<string> Required { get; set; } = new List<string>();
}
