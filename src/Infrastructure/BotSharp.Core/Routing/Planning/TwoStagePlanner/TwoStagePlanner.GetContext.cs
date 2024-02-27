namespace BotSharp.Core.Routing.Planning;

public partial class TwoStagePlanner
{
    public string GetContext()
    {
        var content = "";
        foreach (var c in _executionContext)
        {
            content += $"* {c}\r\n";
        }
        return content;
    }
}
