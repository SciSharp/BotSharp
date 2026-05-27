namespace BotSharp.Abstraction.Templating;

public interface IRenderConfiguration
{
    string Render(IServiceProvider services, string template, IDictionary<string, object> dict);
    void RegisterType(Type type);
}
