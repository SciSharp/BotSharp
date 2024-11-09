using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.VertexAI.Providers;
using LangChain.Providers.Google.VertexAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BotSharp.Plugin.VertexAI
{
    public class VertexAiPlugin : IBotSharpPlugin
    {
        public string Id => "962ff441-2b40-4db4-b530-49efb1688a75";
        public string Name => "VertexAI";
        public string Description => "VertexAI Service including text generation, text to image and other AI services.";
        public string IconUrl => "https://upload.wikimedia.org/wikipedia/commons/thumb/0/05/Vertex_AI_Logo.svg/480px-Vertex_AI_Logo.svg.png";

        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            services.AddScoped(provider =>
            {
                var settingService = provider.GetRequiredService<ISettingService>();
                return settingService.Bind<VertexAIConfiguration>("VertexAI");
            });
            services.AddScoped<IChatCompletion, ChatCompletionProvider>();
            services.AddScoped<ITextCompletion, TextCompletionProvider>();
        }
    }
}
