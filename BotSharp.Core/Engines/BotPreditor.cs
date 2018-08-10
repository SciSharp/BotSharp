using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Core.Models;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines
{
    public class BotPreditor
    {
        public async Task<NlpDoc> Predict(Agent agent, AIRequest request)
        {
            // load model
            var dir = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "ModelFiles", agent.Id);
            Console.WriteLine($"Load model from {dir}");
            var metaJson = File.ReadAllText(Path.Join(dir, "metadata.json"));
            var meta = JsonConvert.DeserializeObject<ModelMetaData>(metaJson);

            // Get NLP Provider
            var config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
            var assemblies = (string[])AppDomain.CurrentDomain.GetData("Assemblies");

            var providerPipe = meta.Pipeline.First();
            var provider = TypeHelper.GetInstance(providerPipe.Name, assemblies) as INlpPipeline;
            provider.Configuration = config.GetSection(meta.Platform);

            var data = new NlpDoc
            {
                Sentences = new List<NlpDocSentence>
                {
                    new NlpDocSentence
                    {
                        Text = request.Query.FirstOrDefault()
                    }
                }
            };

            await provider.Train(agent, data, providerPipe);
            meta.Pipeline.RemoveAt(0);

            var settings = new PipeSettings
            {
                ModelDir = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "ModelFiles", agent.Id),
                PredictDir = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "PredictFiles", agent.Id),
                AlgorithmDir = Path.Join(AppDomain.CurrentDomain.GetData("ContentRootPath").ToString(), "Algorithms")
            };

            if (!Directory.Exists(settings.PredictDir))
            {
                Directory.CreateDirectory(settings.PredictDir);
            }
            
            // pipe process
            meta.Pipeline.ForEach(async pipeMeta =>
            {
                var pipe = TypeHelper.GetInstance(pipeMeta.Name, assemblies) as INlpPipeline;
                pipe.Configuration = provider.Configuration;
                pipe.Settings = settings;
                await pipe.Predict(agent, data, pipeMeta);
            });

            Console.WriteLine(JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));

            return data;
        }
    }
}
