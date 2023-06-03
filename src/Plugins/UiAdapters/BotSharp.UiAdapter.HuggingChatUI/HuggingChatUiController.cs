using BotSharp.Abstraction;
using HuggingChatUI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;

namespace HuggingChatUI;

public class HuggingChatUiController : ControllerBase, IBotUiAdapter
{
    private readonly IPlatformMidware _platform;
    public HuggingChatUiController(IPlatformMidware platform)
    {
        _platform = platform;
    }

    [HttpPost("conversation")]
    public ConversationViewModel NewSession([FromBody] ConversationCreationModel conversationCreationModel)
    {
        var session = _platform.SessionService.NewSession("");
        return new ConversationViewModel
        {
            ConversationId = session.SessionId.Replace("-", "").Substring(0, 24)
        };
    }

    [HttpPost("conversation/{id}/summarize")]
    public string SummarizeTitle([FromRoute] string id)
    {
        return "SummarizeTitle";
    }
}
