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
using BotSharp.Plugin.ChatbotUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using Microsoft.AspNetCore.Authorization;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Abstraction.Routing;

namespace BotSharp.Plugin.ChatbotUI.Controllers;

[Authorize]
[ApiController]
public class ChatbotUiController : ControllerBase
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
        var llm = _services.GetRequiredService<ILlmProviderService>();
        var models = llm.GetProviderModels("azure-openai").Where(x => x.Type == LlmModelType.Chat)
            .Select(x => new AiModel
            {
                Id = x.Id,
                Model = x.Name,
                Name = x.Name
            }).ToList();

        return new OpenAiModels
        {
            Data = models,
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

        var llm = _services.GetRequiredService<ILlmProviderService>();
        var model = llm.GetProviderModels("azure-openai")
            .First(x => x.Type == LlmModelType.Chat && x.Id == input.Model)
            .Name;

        var conv = _services.GetRequiredService<IConversationService>();
        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.SetMessageId(input.ConversationId, message.MessageId);

        conv.SetConversationId(input.ConversationId, input.States);
        conv.States.SetState("channel", input.Channel)
                   .SetState("provider", "azure-openai")
                   .SetState("model", model)
                   .SetState("temperature", input.Temperature)
                   .SetState("sampling_factor", input.SamplingFactor);

        var result = await conv.SendMessage(input.AgentId,
            message,
            replyMessage: null,
            async msg => 
                await OnChunkReceived(outputStream, msg),
            _ => Task.CompletedTask,
            _ => Task.CompletedTask);

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