using BotSharp.Abstraction;
using BotSharp.Abstraction.ApiAdapters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Plugin.HuggingFace.HuggingChat.ViewModels;

namespace BotSharp.Plugin.HuggingFace.HuggingChat;

public class HuggingChatController : ControllerBase, IApiAdapter
{
    private readonly IPlatformMidware _platform;
    public HuggingChatController(IPlatformMidware platform)
    {
        _platform = platform;
    }

    [HttpPost("/conversation")]
    public async Task<ConversationViewModel> NewSession([FromBody] ConversationCreationModel conversationCreationModel)
    {
        var session = await _platform.SessionService.NewSession("anonymous");
        return new ConversationViewModel
        {
            ConversationId = session.SessionId
        };
    }

    [HttpPost("/conversation/{id}/summarize")]
    public string SummarizeTitle([FromRoute] string id)
    {
        return "SummarizeTitle";
    }

    [HttpPost("/models/OpenAssistant/{model}")]
    public async Task SendMessage([FromRoute] string model, [FromBody] ChatInput message)
    {
        Response.StatusCode = 200;
        Response.Headers.Add(HeaderNames.ContentType, "text/event-stream");
        Response.Headers.Add(HeaderNames.CacheControl, "no-cache");
        Response.Headers.Add(HeaderNames.Connection, "keep-alive");
        var outputStream = Response.Body;

        var response = new ChatResponse
        {

        };

        var json = JsonConvert.SerializeObject(response, new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        var buffer = Encoding.UTF8.GetBytes(json);
        await outputStream.WriteAsync(buffer, 0, buffer.Length);
    }
}
