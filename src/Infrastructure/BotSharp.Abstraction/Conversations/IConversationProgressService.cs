namespace BotSharp.Abstraction.Conversations;

public delegate Task FunctionExecuting(RoleDialogModel msg);
public delegate Task FunctionExecuted(RoleDialogModel msg);

public interface IConversationProgressService
{
    FunctionExecuted OnFunctionExecuted { get; set; }
    FunctionExecuting OnFunctionExecuting { get; set; }
}
