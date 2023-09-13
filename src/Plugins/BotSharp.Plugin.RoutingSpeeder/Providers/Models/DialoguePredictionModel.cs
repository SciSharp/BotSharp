using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Plugin.RoutingSpeeder.Providers.Models;

public class DialoguePredictionModel
{
    public int Id { get; set; }
    public string Text { get; set; }
    public string? Label { get; set; }
    public string? Prediction { get; set; }
}
