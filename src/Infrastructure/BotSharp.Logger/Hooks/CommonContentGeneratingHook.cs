public class CommonContentGeneratingHook : IContentGeneratingHook
{
    private readonly IServiceProvider _services;

    public CommonContentGeneratingHook(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// After content generated.
    /// </summary>
    /// <returns></returns>
    public async Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        SaveLlmCompletionLog(message, tokenStats);
        await Task.CompletedTask;
    }

    private void SaveLlmCompletionLog(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        var convSettings = _services.GetRequiredService<ConversationSetting>();
        if (!convSettings.EnableLlmCompletionLog) return;

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var state = _services.GetRequiredService<IConversationStateService>();

        var completionLog = new LlmCompletionLog
        {
            ConversationId = state.GetConversationId(),
            MessageId = message.MessageId,
            AgentId = message.CurrentAgentId,
            Prompt = tokenStats.Prompt,
            Response = message.Content
        };

        db.SaveLlmCompletionLog(completionLog);
    }
}
