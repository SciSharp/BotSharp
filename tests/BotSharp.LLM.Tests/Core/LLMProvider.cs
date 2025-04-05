using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Settings;
using BotSharp.Core;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.AnthropicAI;
using BotSharp.Plugin.AnthropicAI.Settings;
using BotSharp.Plugin.GoogleAi;
using BotSharp.Plugin.GoogleAi.Settings;
using BotSharp.Plugin.OpenAI;
using BotSharp.Plugin.OpenAI.Settings;
using GenerativeAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.Google.Core
{
    public static class LLMProvider
    {
        public static bool CanRunGemini => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_API_KEY"));
        public static bool CanRunOpenAI => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPEN_AI_APIKEY"));
        public static bool CanRunAnthropic => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
        
        private static ILoggerFactory  _loggerFactory = LoggerFactory.Create((builder) => builder.AddConsole());
        public static (IServiceCollection services, IConfiguration config, string modelName) CreateGemini()
        {
            string modelName = GoogleAIModels.Gemini2FlashLitePreview;
            
            var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY") ??
                throw new Exception("GOOGLE_API_KEY is not set");
            var services = new ServiceCollection();
            
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddInMemoryCollection(
                 new Dictionary<string, string?>(new[] { new KeyValuePair<string,string?>("GoogleAi:Gemini:ApiKey",apiKey) })).Build();
            
            LlmProviderSetting setting = new LlmProviderSetting();
            setting.Provider = "google-ai";
            setting.Models = new List<LlmModelSetting>([
                new LlmModelSetting()
                {
                    Name = modelName,
                    ApiKey = apiKey
                },
                new LlmModelSetting()
                {
                    Name = GoogleAIModels.Gemini2FlashExp,
                    ApiKey = apiKey
                },
                new LlmModelSetting()
                {
                    Name = GoogleAIModels.TextEmbedding,
                    ApiKey = apiKey
                }
            ]);
            services.AddSingleton(new GoogleAiSettings()
            {
                Gemini = new GeminiSetting()
                {
                    ApiKey = apiKey
                }
            });
            
            services.AddSingleton<List<LlmProviderSetting>>(new List<LlmProviderSetting>([ setting]));
          
            AddCommonServices(services, configuration);
          
            new GoogleAiPlugin().RegisterDI(services, configuration);
            return (services, configuration, modelName);
        }
        
        public static (IServiceCollection services, IConfiguration config, string modelName) CreateOpenAI()
        {
            string modelName = "gpt-4o-mini";
            
            var apiKey = Environment.GetEnvironmentVariable("OPEN_AI_APIKEY") ??
                         throw new Exception("OPEN_AI_APIKEY is not set");
            var services = new ServiceCollection();
            
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddInMemoryCollection(
                new Dictionary<string, string?>(new[] { new KeyValuePair<string,string?>("GoogleAi:Gemini:ApiKey",apiKey) })).Build();
            
            LlmProviderSetting setting = new LlmProviderSetting();
            setting.Provider = "OpenAi";
            setting.Models = new List<LlmModelSetting>([
                new LlmModelSetting()
                {
                    Name = modelName,
                    ApiKey = apiKey
                },
                new LlmModelSetting()
                {
                    Name = "text-embedding-3-small",
                    ApiKey = apiKey
                }
            ]);
            services.AddSingleton(new OpenAiSettings()
            {
                
            });
            
            services.AddSingleton<List<LlmProviderSetting>>(new List<LlmProviderSetting>([ setting]));
          
            AddCommonServices(services, configuration);
          
            
            new OpenAiPlugin().RegisterDI(services, configuration);
            return (services, configuration, modelName);
        }

        private static void AddCommonServices(ServiceCollection services, IConfigurationRoot configuration)
        {
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IAgentService, TestAgentService>();
            services.AddSingleton<ILlmProviderService, LlmProviderService>();
            services.AddSingleton<ISettingService, SettingService>();
            services.AddSingleton<IConversationStateService, NullConversationStateService>();
            services.AddSingleton<IFileStorageService, NullFileStorageService>();
            services.AddLogging(s=>s.AddConsole());
        }

        public static (IServiceCollection services, IConfiguration configuration, string modelName) CreateAnthropic()
        {
            string modelName = "claude-3-5-haiku-20241022";
            
            var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ??
                         throw new Exception("ANTHROPIC_API_KEY is not set");
            var services = new ServiceCollection();
            
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            
            LlmProviderSetting setting = new LlmProviderSetting();
            setting.Provider = "anthropic";
            setting.Models = new List<LlmModelSetting>([
                new LlmModelSetting()
                {
                    Name = modelName,
                    ApiKey = apiKey
                }
            ]);
            services.AddSingleton(new AnthropicSettings()
            {
                Claude = new ClaudeSetting()
                {
                    
                }
            });
            
            services.AddSingleton<List<LlmProviderSetting>>(new List<LlmProviderSetting>([ setting]));
          
            AddCommonServices(services, configuration);
            
            new AnthropicPlugin().RegisterDI(services, configuration);
            return (services, configuration, modelName);
        }
    }
}