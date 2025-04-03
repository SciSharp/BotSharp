using System.Threading;
using GenerativeAI.Core;
using GenerativeAI.Types;

namespace BotSharp.Plugin.GoogleAi.Providers.Realtime
{
    public class FakeFunctionTool:IFunctionTool
    {
        public Tool Tool { get; set; }

        public FakeFunctionTool(Tool tool)
        {
            this.Tool = tool;
        }
        public Tool AsTool()
        {
           return Tool;
        }

        public async Task<FunctionResponse?> CallAsync(FunctionCall functionCall, CancellationToken cancellationToken = new CancellationToken())
        {
            return null;
        }

        public bool IsContainFunction(string name)
        {
            return false;
        }
    }
}