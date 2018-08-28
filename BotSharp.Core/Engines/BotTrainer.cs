using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Core.Intents;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace BotSharp.Core.Engines
{
    public class BotTrainer
    {
        private Database dc;

        private string agentId;

        public BotTrainer()
        {
        }

        public BotTrainer(string agentId, Database dc)
        {
            this.dc = dc;
            this.agentId = agentId;
        }

        public async Task<ModelMetaData> Train(Agent agent, BotTrainOptions options)
        {
            var data = new NlpDoc();

            // Get NLP Provider
            var config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
            var assemblies = (string[])AppDomain.CurrentDomain.GetData("Assemblies");
            var platform = config.GetSection($"BotPlatform").Value;
            string providerName = config.GetSection($"{platform}:Provider").Value;
            var provider = TypeHelper.GetInstance(providerName, assemblies) as INlpProvider;
            provider.Configuration = config.GetSection(platform);

            var pipeModel = new PipeModel
            {
                Name = providerName,
                Class = provider.ToString(),
                Meta = new JObject(),
                Time = DateTime.UtcNow
            };

            await provider.Load(agent, pipeModel);

            var settings = new PipeSettings
            {
                ProjectDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", agent.Id),
                AlgorithmDir = Path.Combine(AppDomain.CurrentDomain.GetData("ContentRootPath").ToString(), "Algorithms")
            };

            settings.ModelDir = Path.Combine(settings.ProjectDir, String.IsNullOrEmpty(options.Model) ? "model" + DateTime.UtcNow.ToString("MMddyyyyHHmm") : options.Model);

            if (!Directory.Exists(settings.ProjectDir))
            {
                Directory.CreateDirectory(settings.ProjectDir);
            }

            if (!Directory.Exists(settings.TempDir))
            {
                Directory.CreateDirectory(settings.TempDir);
            }

            if (!Directory.Exists(settings.ModelDir))
            {
                Directory.CreateDirectory(settings.ModelDir);
            }

            var meta = new ModelMetaData
            {
                Platform = platform,
                Language = agent.Language,
                TrainingDate = DateTime.UtcNow,
                Version = config.GetValue<String>($"Version"),
                Pipeline = new List<PipeModel>() { pipeModel },
                Model = settings.ModelDir
            };

            // pipe process
            var pipelines = provider.Configuration.GetValue<String>($"Pipe:train")
                .Split(',')
                .Select(x => x.Trim())
                .ToList();

            pipelines.ForEach(async pipeName =>
            {
                var pipe = TypeHelper.GetInstance(pipeName, assemblies) as INlpTrain;
                pipe.Configuration = provider.Configuration;
                pipe.Settings = settings;
                pipeModel = new PipeModel
                {
                    Name = pipeName,
                    Class = pipe.ToString(),
                    Time = DateTime.UtcNow
                };
                meta.Pipeline.Add(pipeModel);

                await pipe.Train(agent, data, pipeModel);
            });

            // save model meta data
            var metaJson = JsonConvert.SerializeObject(meta, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            File.WriteAllText(Path.Combine(settings.ModelDir, "metadata.json"), metaJson);

            Console.WriteLine(metaJson);

            return meta;
        }
    }
}
