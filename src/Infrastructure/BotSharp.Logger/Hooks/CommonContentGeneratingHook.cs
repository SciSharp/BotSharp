using BotSharp.Abstraction.Loggers.Models;

public class CommonContentGeneratingHook : IContentGeneratingHook
{
    private readonly IServiceProvider _services;

    public CommonContentGeneratingHook(IServiceProvider services)
    {
        _services = services;
    }

    public async Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        SaveLlmCompletionLog(message, tokenStats);
        await Task.CompletedTask;
    }

    private void SaveLlmCompletionLog(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        var convSettings = _services.GetRequiredService<ConversationSetting>();
        var conv = _services.GetRequiredService<IConversationService>();

        if (!convSettings.EnableLlmCompletionLog
            || string.IsNullOrEmpty(conv.ConversationId)
            || string.IsNullOrWhiteSpace(tokenStats.Prompt)
            || string.IsNullOrWhiteSpace(message.Content))
        {
            return;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var completionLog = new LlmCompletionLog
        {
            ConversationId = conv.ConversationId,
            MessageId = message.MessageId,
            AgentId = message.CurrentAgentId,
            Prompt = tokenStats.Prompt,
            Response = message.Content,
            CreatedTime = DateTime.UtcNow
        };

        db.SaveLlmCompletionLog(completionLog);
    }
}
