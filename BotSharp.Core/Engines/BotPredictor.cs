using BotSharp.Core.Abstractions;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.AiRequest;
using BotSharp.Platform.Models.MachineLearning;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Console = Colorful.Console;

namespace BotSharp.Core.Engines
{
    public class BotPredictor
    {
        public async Task<NlpDoc> Predict(AgentBase agent, AiRequest request)
        {
            // load model
            var dir = Path.Combine(request.AgentDir, request.Model);
            Console.WriteLine($"Load model from {dir}");
            var metaJson = File.ReadAllText(Path.Combine(dir, "model-meta.json"));
            var meta = JsonConvert.DeserializeObject<ModelMetaData>(metaJson);

            // Get NLP Provider
            var config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
            var assemblies = (string[])AppDomain.CurrentDomain.GetData("Assemblies");

            var providerPipe = meta.Pipeline.First();
            var provider = TypeHelper.GetInstance(providerPipe.Name, assemblies) as INlpProvider;
            provider.Configuration = config.GetSection(meta.Platform);

            var data = new NlpDoc
            {
                Sentences = new List<NlpDocSentence>
                {
                    new NlpDocSentence
                    {
                        Text = request.Text
                    }
                }
            };

            await provider.Load(agent, providerPipe);
            meta.Pipeline.RemoveAt(0);

            var settings = new PipeSettings
            {
                ModelDir = dir,
                ProjectDir = request.AgentDir
            };


            // pipe process
            var pipelines = config.GetValue<String>($"{meta.BotEngine}:pipe")
                .Split(',')
                .Select(x => x.Trim())
                .ToList();

            for(int pipeIdx = 0; pipeIdx < pipelines.Count; pipeIdx++)
            {
                var pipe = TypeHelper.GetInstance(pipelines[pipeIdx], assemblies) as INlpPredict;
                pipe.Configuration = config.GetSection(meta.BotEngine).GetSection(pipelines[pipeIdx]);
                pipe.Settings = settings;
                var pipeModel = meta.Pipeline.FirstOrDefault(x => x.Name == pipelines[pipeIdx]);
                await pipe.Predict(agent, data, pipeModel);
            }

            /*Console.WriteLine($"Prediction result:", Color.Green);
            Console.WriteLine(JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));*/

            return data;
        }
    }
}
