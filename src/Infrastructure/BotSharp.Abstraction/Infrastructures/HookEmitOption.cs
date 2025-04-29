namespace BotSharp.Abstraction.Infrastructures;

public class HookEmitOption<T>
{
    public bool OnlyOnce { get; set; }

    /// <summary>
    /// Optional predicate to determine if the hook action should be executed for a specific hook instance.
    /// </summary>
    public Func<T, bool>? ShouldExecute { get; set; }
}
