namespace BotSharp.Logger.Hooks;

public class VerboseLogHook : IVerboseLogHook
{
    private readonly ConversationSetting _convSettings;
    private readonly ILogger<VerboseLogHook> _logger;

    public VerboseLogHook(ConversationSetting convSettings, ILogger<VerboseLogHook> logger)
    {
        _convSettings = convSettings;
        _logger = logger;
    }

    public void GenerateLog(string text)
    {
        if (!_convSettings.ShowVerboseLog) return;

        _logger.LogInformation(text);
    }
}
