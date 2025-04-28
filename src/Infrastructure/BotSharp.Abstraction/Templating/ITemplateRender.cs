namespace BotSharp.Abstraction.Templating;

public interface ITemplateRender
{
    string Render(string template, Dictionary<string, object> dict);

    /// <summary>
    /// Register tag
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="content">A dictionary whose key is identifier and value is its content to render</param>
    /// <param name="data"></param>
    /// <returns></returns>
    bool RegisterTag(string tag, Dictionary<string, string> content, Dictionary<string, object>? data = null);

    /// <summary>
    /// Register tags
    /// </summary>
    /// <param name="tags">A dictionary whose key is tag and value is its identifier and content to render</param>
    /// <param name="data"></param>
    /// <returns></returns>
    bool RegisterTags(Dictionary<string, List<AgentPromptBase>> tags, Dictionary<string, object>? data = null);
    void RegisterType(Type type);
}
