using BotSharp.Abstraction.MLTasks.Settings;

namespace BotSharp.Abstraction.MLTasks.Utilities;

public static class LlmUtility
{
    public static string? GetModelParameter(IDictionary<string, ModelParamSetting>? settings, string key, string curVal)
    {
        string? res = null;

        if (settings != null
            && settings.TryGetValue(key, out var value)
            && value != null)
        {
            res = VerifyModelParameter(curVal, value.Default, value.Options);
        }

        return res;
    }

    public static string? VerifyModelParameter(string? curVal, string? defaultVal, IEnumerable<string>? options = null)
    {
        if (options.IsNullOrEmpty())
        {
            return curVal.IfNullOrEmptyAs(defaultVal);
        }

        return options!.Contains(curVal) ? curVal : defaultVal;
    }
}
