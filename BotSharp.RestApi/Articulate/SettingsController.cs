using BotSharp.Core.Engines.Articulate;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotSharp.RestApi.Articulate
{
#if ARTICULATE
    [Route("[controller]")]
    public class SettingsController : ControllerBase
    {
        [HttpGet]
        public SettingsModel GetSettings()
        {
            string dataPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate", "settings.json");

            string json = System.IO.File.ReadAllText(dataPath);

            var settings = JsonConvert.DeserializeObject<SettingsModel>(json);

            return settings;
        }
    }
#endif
}
