namespace BotSharp.Abstraction.Routing.Models;

public abstract class InvokeOptions
{
    public string From { get; set; }
}

public class InvokeAgentOptions : InvokeOptions
{
    public bool UseStream { get; set; }

    public static InvokeAgentOptions Default()
    {
        return new()
        {
            From = InvokeSource.Manual,
            UseStream = false
        };
    }
}

public class InvokeFunctionOptions : InvokeOptions
{
    public static InvokeFunctionOptions Default()
    {
        return new()
        {
            From = InvokeSource.Manual
        };
    }
}