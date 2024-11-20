namespace BotSharp.Plugin.ExcelHandler.Hooks;

public class ExcelHandlerHook : AgentHookBase, IAgentHook
{
    private const string HANDLER_EXCEL = "handle_excel_request";

    public override string SelfId => string.Empty;

    public ExcelHandlerHook(IServiceProvider services, AgentSettings settings) : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var utilityLoad = new AgentUtility
        {
            Name = UtilityName.ExcelHandler,
            Content = new UtilityContent
            {
                Functions = [new(HANDLER_EXCEL)],
                Templates = [new($"{HANDLER_EXCEL}.fn")]
            }
        };

        base.OnLoadAgentUtility(agent, [utilityLoad]);
        base.OnAgentLoaded(agent);
    }
}

