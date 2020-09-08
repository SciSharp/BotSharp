using BotSharp.Platform.Dialogflow.Models;
using BotSharp.Platform.Dialogflow.ViewModels;
using BotSharp.Platform.Models.Agents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Platform.Dialogflow.Controllers
{
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
        public AgentController(DialogflowAi<AgentModel> platform)
        {
            builder = platform;
        }

        /// <summary>
        /// Import a agent from a uploaded zip file 
        /// </summary>
        /// <param name="uploadedFile">Upload Zip File， meta.json is the extra data for customized function.</param>
        /// <returns></returns>
        [HttpPost("import")]
        [ProducesResponseType(typeof(ImportedAgentViewModel), 200)]
        public async Task<IActionResult> Import(IFormFile uploadedFile)
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
            if (Directory.Exists(dest))
            {
                System.IO.Directory.Delete(dest, true);
            }

            Console.WriteLine($"Extract zip file to {dest}");
            ZipFile.ExtractToDirectory(filePath, dest);

            System.IO.File.Delete(filePath);

            var agent = await builder.LoadAgentFromFile<AgentImporterInDialogflow<AgentModel>>(dest);
            await builder.SaveAgent(agent);

            return Ok(new ImportedAgentViewModel
            {
                Id = agent.Id,
                Name = agent.Name,
                Description = agent.Description,
                ClientAccessToken = agent.ClientAccessToken
            });
        }

        /// <summary>
        /// Dump agent
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [HttpGet("{agentId}")]
        public ActionResult Dump([FromRoute] String agentId)
        {
            return Ok();
        }
    }
}
