using BotSharp.Abstraction.Infrastructures.Enums;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    private const string TranslationPromptName = "translation_prompt";

    /// <summary>
    /// Normalize an inbound user message to English so conversation hooks, routing rules, agent
    /// instructions and function arguments are always evaluated in English. The user's original
    /// text is kept in SecondaryContent. Runs once in SendMessage ahead of the hook loop, so both
    /// routing paths and every hook observe the same normalized message.
    /// </summary>
    /// <returns>
    /// True when the message was translated and SecondaryContent now holds the user's original text.
    /// Callers rely on this to tell apart a SecondaryContent this method authored from one it never
    /// touched, so it must stay false on every early return.
    /// </returns>
    private async Task<bool> TranslateInboundMessage(Agent agent, RoleDialogModel message)
    {
        var agentSettings = _services.GetRequiredService<AgentSettings>();
        if (!agentSettings.EnableTranslator)
        {
            return false;
        }

        // The caller supplies the language through the request states; the server does not detect it.
        // TranslationService back-fills StateConst.LANGUAGE only when the state is absent, which cannot
        // happen here - an absent state defaults to English and returns early. That back-fill serves
        // the /translate endpoint instead.
        // Unknown is excluded to stay in sync with TranslationResponseHook: it means the language has
        // not been resolved, so translating would only paraphrase the user's own words.
        var language = _state.GetState(StateConst.LANGUAGE, LanguageType.ENGLISH);
        if (language == LanguageType.ENGLISH || language == LanguageType.UNKNOWN)
        {
            return false;
        }

        // TranslationService reads the prompt template off the agent it is handed, and only the
        // AI Assistant defines it. Fall back to that agent when the executing one has no template,
        // which is the normal case for the task agents handled by InstructDirect.
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

        return true;
    }
}
