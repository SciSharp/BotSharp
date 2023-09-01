using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Plugin.RoutingSpeeder.Providers;
using BotSharp.Plugin.RoutingSpeeder.Providers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.RoutingSpeeder.Controllers;

[AllowAnonymous]
public class TrainIntentClassifierController : ControllerBase
{
    private readonly IServiceProvider _service;
    public TrainIntentClassifierController(IServiceProvider service)
    {
        _service = service;
    }

    [HttpPost("/intent/classifier/training")]
    public IActionResult TrainIntentClassifier(TrainingParams trainingParams)
    {
        var intentClassifier = _service.GetRequiredService<IntentClassifier>();
        intentClassifier.InitClassifer(trainingParams.Reference);
        intentClassifier.Train(trainingParams);
        return Ok(intentClassifier.Labels);
    }

}
