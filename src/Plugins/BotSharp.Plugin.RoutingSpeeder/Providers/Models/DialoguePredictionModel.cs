using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Plugin.RoutingSpeeder.Providers.Models;

public class DialoguePredictionModel
{
    public int Id { get; set; }
    public string text { get; set; }
    public string? label { get; set; }
    public string? prediction { get; set; }
}
