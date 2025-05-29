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
                Category = "web",
                Name = "browser.tools",
                Items = [
                    new UtilityItem
                    {
                        FunctionName = GO_TO_PAGE_FN,
                        TemplateName = $"{GO_TO_PAGE_FN}.fn"
                    },
                    new UtilityItem
                    {
                        FunctionName = ACTION_ON_ELEMENT_FN,
                        TemplateName = $"{ACTION_ON_ELEMENT_FN}.fn"
                    },
                    new UtilityItem
                    {
                        FunctionName = LOCATE_ELEMENT_FN
                    },
                    new UtilityItem
                    {
                        FunctionName = CLOSE_BROWSER_FN
                    }
                ]
            }
        };

        utilities.AddRange(items);
    }
}
