using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NRules;
using NRules.RuleSharp;

namespace BotSharp.Core.NRules;

public class NRulesPlugin : IBotSharpPlugin
{
    public string Id => throw new NotImplementedException();

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // 1. 注册规则库（单例）
        services.AddSingleton<RuleRepository>(provider =>
        {
            var repo = new RuleRepository();
            // 关键：注入 BotSharp 核心与抽象层的程序集引用
            repo.AddReference(typeof(BotSharp.Abstraction.Rules.IRuleEngine).Assembly);
            //repo.AddReference(typeof(BotSharp.Core..RuleEngine).Assembly);
            // 注入常用系统库
            repo.AddNamespace("System");
            repo.AddNamespace("System.Linq");
            return repo;
        });

        // 2. 注册规则加载器服务
        services.AddSingleton<IRuleLoader, RuleSharpFileLoader>();

        // 3. 注册编译后的会话工厂（单例）
        services.AddSingleton<ISessionFactory>(provider =>
        {
            var repo = provider.GetRequiredService<RuleRepository>();
            var loader = provider.GetRequiredService<IRuleLoader>();
            // 从指定目录加载所有.rs 文件
            loader.LoadFromDirectory(repo, "Settings/Rules");
            return repo.Compile();
        });

        // 5. 注册 Hook 以拦截对话
        services.AddScoped<IConversationHook, RuleInjectionHook>();
        services.AddSingleton<INRulesEngineService, NRulesEngineService>();
    }
}