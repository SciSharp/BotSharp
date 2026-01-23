using BotSharp.Abstraction.Instructs.Options;
using NRules;

namespace BotSharp.Abstraction.Rules;

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

public interface IUniversalParsingEngine
{
    // 获取或创建当前会话的 BotsharpRuleContext
    Task<IRuleContext> GetContextAsync(string conversationId);

    // 加载并编译规则
    Task LoadRules();

    Task<T?> ParseAsync<T>(string text, InstructOptions? options = null) where T : class;
}
