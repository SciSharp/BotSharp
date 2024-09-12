namespace BotSharp.Core.Conversations.Services
{
    public class ConversationProgressService : IConversationProgressService
    {

        public FunctionExecuting OnFunctionExecuting { get; set; }


        public FunctionExecuted OnFunctionExecuted { get; set; }
    }
}
