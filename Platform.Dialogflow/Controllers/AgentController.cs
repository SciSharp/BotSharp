using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Platform.Dialogflow.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Dialogflow.Controllers
{
#if DIALOGFLOW
    /// <summary>
    /// Agent
    /// </summary>
    [Route("v1/[controller]")]
    public class AgentController : ControllerBase
    {
        private DialogflowAi<AgentModel> builder;

        /// <summary>
        /// Initialize dialog controller and get a platform instance
        /// </summary>
        /// <param name="platform"></param>
        public AgentController(IConfiguration configuration)
        {
            builder = new DialogflowAi<AgentModel>();
            builder.PlatformConfig = configuration.GetSection("DialogflowAi");
        }

        /// <summary>
        /// Import a agent from a uploaded zip file 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile uploadedFile)
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

            string dest = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", uploadedFile.FileName.Split('.').First(), "tmp");
            System.IO.Directory.Delete(dest, true);

            Console.WriteLine($"Extract zip file to {dest}");
            ZipFile.ExtractToDirectory(filePath, dest);

            System.IO.File.Delete(filePath);

            Console.WriteLine($"LoadAgentFromFile {dest}");
            var agent = builder.LoadAgentFromFile<AgentImporterInDialogflow<AgentModel>>(dest);

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
            return Ok();
        }
    }
#endif
}
