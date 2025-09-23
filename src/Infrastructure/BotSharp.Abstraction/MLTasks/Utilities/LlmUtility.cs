namespace BotSharp.Abstraction.MLTasks.Utilities;

public static class LlmUtility
{
    public static string? VerifyModelParameter(string? curVal, string? defaultVal, IEnumerable<string>? options = null)
    {
        if (options.IsNullOrEmpty())
        {
            return curVal.IfNullOrEmptyAs(defaultVal);
        }

        return options.Contains(curVal) ? curVal : defaultVal;
    }
}
