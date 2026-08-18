using BotSharp.Abstraction.Infrastructures.Enums;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    private const string TranslationPromptName = "translation_prompt";

    /// <summary>
    /// Normalize an inbound user message to English so routing rules, agent instructions and
    /// function arguments are always evaluated in English. The user's original text is kept in
    /// SecondaryContent. Shared by InstructLoop and InstructDirect so every entry point into the
    /// routing service behaves the same way.
    /// </summary>
    private async Task TranslateInboundMessage(Agent agent, RoleDialogModel message)
    {
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        if (!agentSettings.EnableTranslator)
        {
            return;
        }

        var states = _services.GetRequiredService<IConversationStateService>();

        // The caller supplies the language through the request states; the server does not detect it.
        // TranslationService back-fills StateConst.LANGUAGE only when the state is absent, which cannot
        // happen here - an absent state defaults to English and returns early. That back-fill serves
        // the /translate endpoint instead.
        // Unknown is excluded to stay in sync with TranslationResponseHook: it means the language has
        // not been resolved, so translating would only paraphrase the user's own words.
        var language = states.GetState(StateConst.LANGUAGE, LanguageType.ENGLISH);
        if (language == LanguageType.ENGLISH || language == LanguageType.UNKNOWN)
        {
            return;
        }

        // TranslationService reads the prompt template off the agent it is handed, and only the
        // AI Assistant defines it. Fall back to that agent when the executing one has no template,
        // which is the normal case for the task agents reaching us through InstructDirect.
        var host = agent;
        if (host?.Templates?.Any(x => x.Name == TranslationPromptName) != true)
        {
            var agentService = _services.GetRequiredService<IAgentService>();
            host = await agentService.LoadAgent(BuiltInAgentId.AIAssistant);
        }

        var translator = _services.GetRequiredService<ITranslationService>();
        message.SecondaryContent = message.Content;
        message.Content = await translator.Translate(host, message.MessageId, message.Content,
            language: LanguageType.ENGLISH,
            clone: false);
    }
}
