using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace BotSharp.Plugin.SemanticKernel
{
    public class SemanticKernelPlugin : IBotSharpPlugin
    {
        public string Name => "Semantic Kernel";
        public string Description => "Semantic Kernel Service";
        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<ITextCompletion, SemanticKernelTextCompletionProvider>();
            services.AddScoped<IChatCompletion, SemanticKernelChatCompletionProvider>();
        }
    }
}
