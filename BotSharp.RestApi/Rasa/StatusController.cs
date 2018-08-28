using BotSharp.Core.Engines;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BotSharp.RestApi.Rasa
{
#if RASA_UI
    /// <summary>
    /// This returns all the currently available projects.
    /// </summary>
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly IBotPlatform _platform;

        /// <summary>
        /// Initialize status controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public StatusController(IBotPlatform platform)
        {
            _platform = platform;
        }

        /// <summary>
        /// Returns a list of available projects the server can use to fulfill /parse requests.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<RasaVersionModel> Get()
        {
            var status = new RasaStatusModel();
            status.AvailableProjects = JObject.FromObject(new { });

            // scan dir, get all models
            var projectPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects");
            var projectDirs = Directory.GetDirectories(projectPath);
            for(int idx = 0; idx < projectDirs.Length; idx++)
            {
                string project = projectDirs[idx].Split('\\').Last();
                var modelDirs = Directory.GetDirectories(projectDirs[idx]);

                List<string> availableModels = new List<string>();

                for (int mIdx = 0; mIdx < modelDirs.Length; mIdx++)
                {
                    string model = modelDirs[mIdx].Split('\\').Last();
                    if (model.StartsWith(project + "_"))
                    {
                        availableModels.Add(model);
                    }
                }

                status.AvailableProjects.Add(project, JObject.FromObject(new RasaProjectModel
                {
                    Status = "ready",
                    AvailableModels = availableModels
                }));
            }

            return Ok(status);
        }
    }
#endif
}
