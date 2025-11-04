namespace BotSharp.Abstraction.Templating;

public interface ITemplateRender
{
    string Render(string template, IDictionary<string, object> dict);
    void RegisterType(Type type);
}
