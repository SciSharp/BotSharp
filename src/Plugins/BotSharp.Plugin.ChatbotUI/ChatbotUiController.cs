using BotSharp.Abstraction.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Plugin.ChatbotUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using Microsoft.AspNetCore.Authorization;
using BotSharp.Abstraction.Agents.Enums;

namespace BotSharp.Plugin.ChatbotUI.Controllers;

[Authorize]
[ApiController]
public class ChatbotUiController : ControllerBase, IApiAdapter
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ChatbotUiController> _logger;

    public ChatbotUiController(ILogger<ChatbotUiController> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    [HttpGet("/v1/models")]
    public OpenAiModels GetOpenAiModels()
    {
        return new OpenAiModels
        {
            Data = new List<AiModel>
            {
                new AiModel
                {
                    Id = "gpt-3.5-turbo",
                    Model = "gpt-3.5-turbo",
                    Name = "GPT-3.5 Turbo",
                    MaxLength = 4 * 1024,
                    TokenLimit = 4 * 1024
                },
                new AiModel
                {
                    Id = "gpt-4",
                    Model = "gpt-4",
                    Name = "GPT-4",
                    MaxLength = 8 * 1024,
                    TokenLimit = 8 * 1024
                }
            }
        };
    }

    [HttpPost("/v1/chat/completions")]
    public async Task SendMessage([FromBody] OpenAiMessageInput input)
    {
        Response.StatusCode = 200;
        Response.Headers.Add(HeaderNames.ContentType, "text/event-stream");
        Response.Headers.Add(HeaderNames.CacheControl, "no-cache");
        Response.Headers.Add(HeaderNames.Connection, "keep-alive");
        var outputStream = Response.Body;

        var message = input.Messages
            .Where(x => x.Role == AgentRole.User)
            .Select(x => new RoleDialogModel(x.Role, x.Content))
            .Last();

        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(input.ConversationId, input.States);
        conv.States.SetState("provider", input.Provider)
            .SetState("model", input.Model)
            .SetState("channel", input.Channel)
            .SetState("temperature", input.Temperature)
            .SetState("sampling_factor", input.SamplingFactor);

        var result = await conv.SendMessage(input.AgentId, 
            message, 
            async msg => 
                await OnChunkReceived(outputStream, msg),
            async fn 
                => await Task.CompletedTask,
            async fn
                => await Task.CompletedTask);

        await OnEventCompleted(outputStream);
    }

    private async Task OnChunkReceived(Stream outputStream, RoleDialogModel message)
    {
        var response = new OpenAiChatOutput
        {
            Choices = new List<OpenAiChoice>
            {
                new OpenAiChoice
                {
                    Delta = new RoleDialogModel(message.Role, message.Content)
                }
            }
        };
        var json = JsonConvert.SerializeObject(response, new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        var buffer = Encoding.UTF8.GetBytes($"data:{json}\n");
        await outputStream.WriteAsync(buffer, 0, buffer.Length);
        await Task.Delay(10);

        buffer = Encoding.UTF8.GetBytes("\n");
        await outputStream.WriteAsync(buffer, 0, buffer.Length);
    }

    private async Task OnEventCompleted(Stream outputStream)
    {
        var buffer = Encoding.UTF8.GetBytes("data:[DONE]\n");
        await outputStream.WriteAsync(buffer, 0, buffer.Length);

        buffer = Encoding.UTF8.GetBytes("\n");
        await outputStream.WriteAsync(buffer, 0, buffer.Length);

        await outputStream.FlushAsync();
    }
}