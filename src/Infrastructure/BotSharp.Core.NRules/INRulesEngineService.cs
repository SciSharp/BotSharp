using Antlr4.Runtime;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Rules;
using Microsoft.Extensions.DependencyInjection;
using NRules;
using NRules.RuleSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.NRules;

public interface INRulesEngineService
{
    // 获取或创建当前会话的 BotsharpRuleContext
    Task<IRuleContext> GetContextAsync(string conversationId);

    // 加载并编译规则
    Task LoadRules();
}

public class NRulesEngineService : INRulesEngineService
{
    private readonly ISessionFactory _sessionFactory;
    private readonly IServiceProvider _services;

    // 使用单例的 Repository 和 Factory 以避免重复编译开销
    public NRulesEngineService(ISessionFactory sessionFactory, IServiceProvider services)
    {
        _sessionFactory = sessionFactory;
        _services = services;
    }

    public async Task<IRuleContext> GetContextAsync(string conversationId)
    {
        // 创建 NRules Session
        var session = _sessionFactory.CreateSession();

        // 获取 BotSharp 的状态服务
        var stateService = _services.GetRequiredService<IConversationStateService>();

        // 实例化 BotsharpRuleContext 封装
        return new BotsharpRuleContext(session, stateService, conversationId);
    }

    public async Task LoadRules()
    {
        var repo = _services.GetRequiredService<RuleRepository>();
        var loader = _services.GetRequiredService<IRuleLoader>();
        // 从指定目录加载所有.rs 文件
        loader.LoadFromDirectory(repo, "Settings/Rules");
        repo.Compile();
    }
}