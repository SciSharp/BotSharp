using BotSharp.Abstraction.Conversations;
using NRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.NRules;

/// <summary>
/// 定义规则引擎与对话状态交互的上下文接口
/// </summary>
public interface IRuleContext
{
    /// <summary>
    /// 获取底层的 NRules 会话对象
    /// </summary>
    ISession Session { get; }

    /// <summary>
    /// 向工作内存中插入事实
    /// </summary>
    void Insert<T>(T fact);

    /// <summary>
    /// 批量插入事实
    /// </summary>
    void InsertAll(IEnumerable<object> facts);

    /// <summary>
    /// 执行规则推理
    /// </summary>
    /// <returns>触发的规则数量</returns>
    int Fire();
 

    /// <summary>
    /// 将 BotSharp 的当前对话状态转换为事实并加载到内存
    /// </summary>
    Task HydrateFactsAsync();

    /// <summary>
    /// 将规则推理产生的状态变更回写到 BotSharp
    /// </summary>
    Task PersistStateAsync();
}

/// <summary>
/// BotsharpRuleContext 具体实现类
/// </summary>
public class BotsharpRuleContext : IRuleContext
{
    private readonly ISession _session;
    private readonly IConversationStateService _stateService;
    private readonly string _conversationId;

    public BotsharpRuleContext(ISession session, IConversationStateService stateService, string conversationId)
    {
        _session = session;
        _stateService = stateService;
        _conversationId = conversationId;
    }

    public ISession Session => _session;

    public void Insert<T>(T fact) => _session.Insert(fact);

    public void InsertAll(IEnumerable<object> facts) => _session.InsertAll(facts);

    public int Fire() => _session.Fire();

    public void Retract(object fact) => _session.Retract(fact);

    public async Task HydrateFactsAsync()
    {
        // 1. 获取 BotSharp 所有状态
        var states = _stateService.GetStates();

        // 2. 使用 FactMapper (需自定义实现) 将 Dictionary 转换为 POCO
        // 示例：将 "user_level": "vip" 映射为 Customer 对象
        var facts = FactMapper.Map(states);

        // 3. 插入会话
        _session.InsertAll(facts);
    }

    public async Task PersistStateAsync()
    {
        // 1. 查询工作内存中特定的状态更新对象 (BotStateUpdate)
        var updates = _session.Query<BotStateUpdate>();

        // 2. 回写到 BotSharp
        foreach (var update in updates)
        {
            _stateService.SetState(update.Key, update.Value);
        }

        // 3. 清理瞬态事实 (Transient Facts)
        // (可选策略：撤回本次插入的所有事实，保持 Session 纯净，依赖外部持久化)
    }
}