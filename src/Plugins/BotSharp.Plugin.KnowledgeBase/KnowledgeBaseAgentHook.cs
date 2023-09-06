using System;
namespace BotSharp.Plugin.KnowledgeBase;

public class KnowledgeBaseAgentHook : AgentHookBase
{
    public KnowledgeBaseAgentHook(IServiceProvider services, AgentSettings settings) 
        : base(services, settings)
    {
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        // Get relevant domain knowledge
        /*if (_settings.EnableKnowledgeBase)
        {
            var knowledge = _services.GetRequiredService<IKnowledgeService>();
            agent.Knowledges = await knowledge.GetKnowledges(new KnowledgeRetrievalModel
            {
                AgentId = agentId,
                Question = string.Join("\n", wholeDialogs.Select(x => x.Content))
            });
        }*/

        return base.OnInstructionLoaded(template, dict);
    }
}
