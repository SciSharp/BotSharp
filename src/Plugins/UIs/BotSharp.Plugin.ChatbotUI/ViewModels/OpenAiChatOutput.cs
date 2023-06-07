using System.Collections.Generic;

namespace BotSharp.Plugin.ChatbotUI.ViewModels;

public class OpenAiChatOutput
{
    public List<OpenAiChoice> Choices { get; set; }
}
