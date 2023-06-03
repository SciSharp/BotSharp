using System.Collections.Generic;

namespace ChatbotUI.ViewModels;

public class OpenAiChatOutput
{
    public List<OpenAiChoice> Choices { get; set; }
}
