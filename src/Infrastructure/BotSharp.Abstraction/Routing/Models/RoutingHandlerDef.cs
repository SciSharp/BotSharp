using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Routing.Models;

public class RoutingHandlerDef
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<ParameterPropertyDef> Parameters { get; set; }

    public override string ToString()
        => $"{Name}: {Description} ({Parameters.Count} Parameters)";
}
