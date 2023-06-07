using BotSharp.Abstraction;
using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Plugin.HuggingChatUI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BotSharp.Plugin.HuggingChatUI.Controllers;

public class HuggingChatUiController : ControllerBase, IApiAdapter
{
    private readonly IPlatformMidware _platform;
    public HuggingChatUiController(IPlatformMidware platform)
    {
        _platform = platform;
    }

    [HttpPost("conversation")]
    public async Task<ConversationViewModel> NewSession([FromBody] ConversationCreationModel conversationCreationModel)
    {
        var session = await _platform.SessionService.NewSession("anonymous");
        return new ConversationViewModel
        {
            ConversationId = session.SessionId
        };
    }

    [HttpPost("conversation/{id}/summarize")]
    public string SummarizeTitle([FromRoute] string id)
    {
        return "SummarizeTitle";
    }
}
