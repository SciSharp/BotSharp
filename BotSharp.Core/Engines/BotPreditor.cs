using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Core.Models;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public async Task<string> Predict(Agent agent, AIRequest request)
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

            var data = JObject.FromObject(new
            {
            });

            await provider.Train(agent, data, providerPipe);
            meta.Pipeline.RemoveAt(0);

            // pipe process
            meta.Pipeline.ForEach(async pipeMeta =>
            {
                var pipe = TypeHelper.GetInstance(pipeMeta.Name, assemblies) as INlpPipeline;
                pipe.Configuration = provider.Configuration;
                await pipe.Predict(agent, data, pipeMeta);
            });

            return "";
        }
    }
}
