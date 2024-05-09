using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Translation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Logger.Hooks
{
    public class TranslationResponseHook : ConversationHookBase
    {
        private readonly IServiceProvider _services;
        private readonly IConversationStateService _states;

        public TranslationResponseHook(IServiceProvider services,
            IConversationStateService states)
        {
            _services = services;
            _states = states;
        }
        public override async Task OnResponseGenerated(RoleDialogModel message)
        {
            // Handle multi-language for output
            var agentService = _services.GetRequiredService<IAgentService>();
            var router = await agentService.LoadAgent(message.CurrentAgentId);
            var translator = _services.GetRequiredService<ITranslationService>();
            var language = _states.GetState("language", LanguageType.ENGLISH);
            if (language != LanguageType.UNKNOWN && language != LanguageType.ENGLISH)
            {
                if (message.RichContent != null)
                {
                    if (string.IsNullOrEmpty(message.RichContent.Message.Text))
                    {
                        message.RichContent.Message.Text = message.Content;
                    }

                    message.SecondaryRichContent = await translator.Translate(router,
                        message.MessageId,
                        message.RichContent,
                        language: language);
                }
                else
                {
                    message.SecondaryContent = await translator.Translate(router,
                        message.MessageId,
                        message.Content,
                        language: language);
                }
            }
            await base.OnResponseGenerated(message);
        }
    }
}
