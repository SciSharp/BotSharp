using BotSharp.Abstraction;
using BotSharp.Abstraction.Models;
using ChatbotUI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace BotSharp.Platform.AzureAi.Controllers;

[ApiController]
public class ChatbotUiController : ControllerBase, IBotUiAdapter
{
    private readonly ILogger<ChatbotUiController> _logger;

    public ChatbotUiController(ILogger<ChatbotUiController> logger)
    {
        _logger = logger;
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
}