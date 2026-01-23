using BotSharp.Abstraction.Conversations;
using NRules;
using NRules.Json;
using NRules.RuleModel;
using System.Text.Json;

namespace BotSharp.Core.NRules;

public class RuleSessionManager
{
    private readonly IConversationStateService _stateService;

    public RuleSessionManager(IConversationStateService stateService)
    {
        _stateService = stateService;
    }

    public void SaveSession(ISession session)
    {
        // 提取所有事实
        var facts = session.Query<object>().ToArray();

        var options = new JsonSerializerOptions { WriteIndented = false };
        // 配置 NRules.Json 的序列化器
        RuleSerializer.Setup(options);

        var json = JsonSerializer.Serialize(facts, options);
        // 存储到 BotSharp 的对话状态中
        _stateService.SetState("RULES_MEMORY_SNAPSHOT", json);
    }

    public void RestoreSession(ISession session)
    {
        var json = _stateService.GetState("RULES_MEMORY_SNAPSHOT");
        if (!string.IsNullOrEmpty(json))
        {
            var options = new JsonSerializerOptions();
            RuleSerializer.Setup(options);

            // 反序列化为对象列表
            var facts = JsonSerializer.Deserialize<IEnumerable<object>>(json, options);

            // 批量插入回 Session
            session.InsertAll(facts);
        }
    }
}