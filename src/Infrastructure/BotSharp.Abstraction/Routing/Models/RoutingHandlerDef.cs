using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction.Routing.Models;

public class RoutingHandlerDef
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<NameDesc> Parameters { get; set; }
}
