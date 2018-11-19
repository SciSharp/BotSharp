using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Rasa.Models
{
    public class RasaVersionModel
    {
        public string Version { get; set; }

        [JsonProperty("minimum_compatible_version")]
        public string MinimumCompatibleVersion { get; set; }
    }
}
