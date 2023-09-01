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
public class RoutingSpeederController : ControllerBase
{
    private readonly IServiceProvider _service;
    public RoutingSpeederController(IServiceProvider service)
    {
        _service = service;
    }

    [HttpPost("/routingspeeder/classifier/train")]
    public IActionResult TrainIntentClassifier(TrainingParams trainingParams)
    {
        var intentClassifier = _service.GetRequiredService<IntentClassifier>();
        intentClassifier.InitClassifer(trainingParams.Inference);
        intentClassifier.Train(trainingParams);
        return Ok(intentClassifier.Labels);
    }

}
