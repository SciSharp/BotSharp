namespace BotSharp.Abstraction.Templating;

public interface ITemplateRender
{
    bool Render(Agent agent, Dictionary<string, object> dict);
}
