namespace BotSharp.Plugin.WebDriver.Hooks;

public class WebUtilityHook : IAgentUtilityHook
{
    private const string PREFIX = "util-web-";
    private const string CLOSE_BROWSER_FN = $"{PREFIX}close_browser";
    private const string GO_TO_PAGE_FN = $"{PREFIX}go_to_page";
    private const string LOCATE_ELEMENT_FN = $"{PREFIX}locate_element";
    private const string ACTION_ON_ELEMENT_FN = $"{PREFIX}action_on_element";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var items = new List<AgentUtility>
        {
            new AgentUtility
            {
                Name = "web.tools",
                Functions = 
                [
                    new(CLOSE_BROWSER_FN),
                    new(GO_TO_PAGE_FN),
                    new(LOCATE_ELEMENT_FN),
                    new(ACTION_ON_ELEMENT_FN)
                ],
                Templates = 
                [
                    new($"{GO_TO_PAGE_FN}.fn"),
                    new($"{ACTION_ON_ELEMENT_FN}.fn")
                ]
            }
        };

        utilities.AddRange(items);
    }
}
