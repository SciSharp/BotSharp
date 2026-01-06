namespace BotSharp.Abstraction.Coding.Utils;

public static class CodingUtil
{
    /// <summary>
    /// Get code execution config => (useLock, useProcess, timeout seconds)
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="defaultTimeoutSeconds"></param>
    /// <returns></returns>
    public static (bool, bool, int) GetCodeExecutionConfig(CodingSettings settings, int defaultTimeoutSeconds = 3)
    {
        var codeExecution = settings.CodeExecution;

        var useLock = codeExecution?.UseLock ?? false;
        var useProcess = codeExecution?.UseProcess ?? false;
        var timeoutSeconds = codeExecution?.TimeoutSeconds > 0 ? codeExecution.TimeoutSeconds : defaultTimeoutSeconds;

        return (useLock, useProcess, timeoutSeconds);
    }
}
