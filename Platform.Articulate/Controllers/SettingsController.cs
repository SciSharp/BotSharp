using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Platform.Articulate.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Platform.Articulate.Controllers
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

        [HttpGet("/agent/{agentId}/settings")]
        public SettingsModel GetSettingsByAgent([FromRoute] string agentId)
        {
            string dataPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate", "settings.json");

            string json = System.IO.File.ReadAllText(dataPath);

            var settings = JsonConvert.DeserializeObject<SettingsModel>(json);

            return settings;
        }

        [HttpPut("/agent/{agentId}/settings")]
        public SettingsModel PutSettingsByAgent([FromRoute] string agentId)
        {
            string dataPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Articulate", "settings.json");

            string json = System.IO.File.ReadAllText(dataPath);

            var settings = JsonConvert.DeserializeObject<SettingsModel>(json);

            return settings;
        }
    }
#endif
}
