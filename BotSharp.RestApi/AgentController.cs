using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using BotSharp.Core.Engines.BotSharp;
using BotSharp.Core.Models;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.RestApi
{
    /// <summary>
    /// Agent
    /// </summary>
    [Route("v1/[controller]/[action]")]
    public class AgentController : ControllerBase
    {
        private readonly IBotPlatform _platform;

        /// <summary>
        /// Initialize dialog controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public AgentController(IBotPlatform platform)
        {
            _platform = platform;
        }

        [HttpGet]
        public ActionResult<List<Agent>> AllAgents()
        {
            List<Agent> agents = new List<Agent>();

            string agentDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects");

            var names = Directory.EnumerateDirectories(agentDir).Select(x => x.Split(Path.DirectorySeparatorChar).Last()).ToList();

            names.ForEach(name =>
            {
                agents.Add(new Agent
                {
                    Name = name
                });

            });

            return agents;
        }

        /// <summary>
        /// Restore a agent from a uploaded zip file 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Restore(IFormFile uploadedFile)
        {
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                return BadRequest();
            }

            var filePath = Path.GetTempFileName();
            Console.WriteLine($"Temp file saved to {filePath}");

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await uploadedFile.CopyToAsync(stream);
            }

            string dest = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", uploadedFile.FileName.Split('.').First(), "model_" + DateTime.UtcNow.ToString("MMddyyyyHHmm"));

            Console.WriteLine($"Extract zip file to {dest}");
            ZipFile.ExtractToDirectory(filePath, dest);

            System.IO.File.Delete(filePath);

            Console.WriteLine($"LoadAgentFromFile {dest}");
            var agent = _platform.LoadAgentFromFile(dest);

#if DIALOGFLOW
            _platform.SaveAgentToDb();
#endif

            return Ok(agent.Id);
        }

        /// <summary>
        /// Dump agent
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [HttpGet("{agentId}")]
        public ActionResult<Agent> Dump([FromRoute] String agentId)
        {
            var agent = _platform.LoadAgent(agentId);

            return Ok(agent);
        }
    }
}
