using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Plugin.RoutingSpeeder.Providers;
using BotSharp.Plugin.RoutingSpeeder.Providers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.RoutingSpeeder.Controllers;

[AllowAnonymous]
public class RoutingSpeederController : ControllerBase
{
    private readonly IServiceProvider _service;
    public RoutingSpeederController(IServiceProvider service)
    {
        _service = service;
    }

    [HttpPost("/routing-speeder/classifier/train")]
    public IActionResult TrainIntentClassifier(TrainingParams trainingParams)
    {
        var intentClassifier = _service.GetRequiredService<IntentClassifier>();
        // intentClassifier.InitClassifer(trainingParams.Inference);
        intentClassifier.Train(trainingParams);
        return Ok(intentClassifier.Labels);
    }

    [HttpPost("/routing-speeder/classifier/inference")]
    public IActionResult TrainIntentClassifier([FromBody] DialoguePredictionModel message)
    {
        var intentClassifier = _service.GetRequiredService<IntentClassifier>();
        var vector = intentClassifier.GetTextEmbedding(message.Text);
        var predText = intentClassifier.Predict(vector);
        return Ok(predText);
    }
}
