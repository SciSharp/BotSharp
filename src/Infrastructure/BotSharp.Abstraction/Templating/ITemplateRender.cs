namespace BotSharp.Abstraction.Templating;

public interface ITemplateRender
{
    string Render(string template, Dictionary<string, object> dict);
    void Register(Type type);
}
