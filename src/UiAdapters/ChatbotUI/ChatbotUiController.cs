using BotSharp.Abstraction;
using BotSharp.Abstraction.Models;
using ChatbotUI.ViewModels;
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
using Azure.AI.OpenAI;

namespace BotSharp.Platform.AzureAi.Controllers;

[ApiController]
public class ChatbotUiController : ControllerBase, IBotUiAdapter
{
    private readonly ILogger<ChatbotUiController> _logger;
    private readonly IPlatformMidware _platform;

    public ChatbotUiController(ILogger<ChatbotUiController> logger,
        IPlatformMidware platform)
    {
        _logger = logger;
        _platform = platform;
    }

    [HttpGet("/v1/models")]
    [HttpGet("/openai/deployments")]
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
                    Name = "Default (GPT-3.5)",
                    MaxLength = 4000,
                    TokenLimit = 4000
                }
            }
        };
    }

    [HttpPost("/v1/chat/completions")]
    [HttpPost("/openai/deployments/one/chat/completions")]
    public async Task SendMessage([FromBody] OpenAiMessageInput input, [FromQuery(Name = "api-version")] string apiVersion = "2023-03-15-preview")
    {
        Response.StatusCode = 200;
        Response.Headers.Add(HeaderNames.ContentType, "text/event-stream");
        Response.Headers.Add(HeaderNames.CacheControl, "no-cache");
        Response.Headers.Add(HeaderNames.Connection, "keep-alive");
        var outputStream = Response.Body;

        await _platform.GetChatCompletionsAsync(input.Messages.Last().Content, async (content, end) =>
        {
            if (end)
            {
                if (content.Length > 0)
                {
                    await OnChunkReceived(outputStream, content);
                }

                await OnEventCompleted(outputStream);
            }
            else
            {
                await OnChunkReceived(outputStream, content);
            }
        });
    }

    private async Task OnChunkReceived(Stream outputStream, string content)
    {

        var response = new OpenAiChatOutput
        {
            Choices = new List<OpenAiChoice>
            {
                new OpenAiChoice
                {
                    Delta = new ChatMessage(ChatRole.Assistant, content)
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
        await Task.Delay(100);

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