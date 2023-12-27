using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using BotSharp.Plugin.HuggingFace.HuggingChat.ViewModels;
using BotSharp.Abstraction.TextGeneratives;

namespace BotSharp.Plugin.HuggingFace.HuggingChat;

public class HuggingChatController : ControllerBase
{
    public HuggingChatController()
    {
    }

    /*[HttpPost("/conversation")]
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
    }*/

    [HttpPost("/models/OpenAssistant/{model}")]
    public async Task SendMessage([FromRoute] string model, [FromBody] ChatInput message)
    {
        if (message.Stream)
        {
            Response.StatusCode = 200;
            Response.Headers.Add(HeaderNames.ContentType, "text/event-stream");
            Response.Headers.Add(HeaderNames.CacheControl, "no-cache");
            Response.Headers.Add(HeaderNames.Connection, "keep-alive");
            var outputStream = Response.Body;

            var response = new ChatResponse
            {
                Token = new TextToken
                {
                    Id = 12092,
                    Text = "Hello"
                }
            };

            var json = JsonConvert.SerializeObject(response, new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var buffer = Encoding.UTF8.GetBytes($"data:{json}\n");
            await outputStream.WriteAsync(buffer, 0, buffer.Length);

            await Task.Delay(100);

            buffer = Encoding.UTF8.GetBytes("\n");
            await outputStream.WriteAsync(buffer, 0, buffer.Length);

            response = new ChatResponse
            {
                Token = new TextToken
                {
                    Text = "<|endoftext|>",
                    Special = true
                },
                GeneratedText = "Hello there! How can I assist you today?"
            };

            json = JsonConvert.SerializeObject(response, new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            buffer = Encoding.UTF8.GetBytes($"data:{json}\n");
            await outputStream.WriteAsync(buffer, 0, buffer.Length);

            await Task.Delay(100);

            buffer = Encoding.UTF8.GetBytes("\n");
            await outputStream.WriteAsync(buffer, 0, buffer.Length);
        }
        else
        {
            Response.StatusCode = 200;
            Response.Headers.Add(HeaderNames.ContentType, "application/json");
            Response.Headers.Add(HeaderNames.CacheControl, "no-cache");
            Response.Headers.Add(HeaderNames.Connection, "keep-alive");
            var outputStream = Response.Body;

            var response = new ChatResponse
            {
                Token = new TextToken
                {
                    Text = "<|endoftext|>",
                    Special = true
                },
                GeneratedText = "Hello there! How can I assist you today?"
            };

            var json = JsonConvert.SerializeObject(new List<ChatResponse>
            {
                response
            }, new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var buffer = Encoding.UTF8.GetBytes(json);
            await outputStream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}
