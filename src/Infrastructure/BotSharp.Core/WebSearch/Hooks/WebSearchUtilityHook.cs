namespace BotSharp.Core.WebSearch.Hooks;

public class WebSearchUtilityHook : IAgentUtilityHook
{
    private const string WEB_INTELLIGENT_SEARCH_FN = "util-web-intelligent_search";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        utilities.Add(new AgentUtility
        {
            Category = "web",
            Name = "intelligent.search",
            Items = [
                new UtilityItem
                {
                    FunctionName = WEB_INTELLIGENT_SEARCH_FN,
                    TemplateName = $"{WEB_INTELLIGENT_SEARCH_FN}.fn"
                }
            ]
        });
    }
}
