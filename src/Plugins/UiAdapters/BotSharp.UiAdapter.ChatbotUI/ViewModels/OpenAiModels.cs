using BotSharp.Abstraction.Models;
using System.Collections.Generic;

namespace ChatbotUI.ViewModels;

public class OpenAiModels
{
    public List<AiModel> Data { get; set; } = new List<AiModel>();
}
