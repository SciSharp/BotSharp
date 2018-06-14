using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotSharp.Core.Abstractions;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Newtonsoft.Json.Linq;

namespace BotSharp.Core.Engines
{
    public class BotTrainer
    {
        private Database dc;

        private string agentId;

        private string config;

        public BotTrainer(string agentId, Database dc, string config = "BotSharpAi")
        {
            this.dc = dc;
            this.agentId = agentId;
            this.config = config;
        }

        public string Train()
        {
            var data = JObject.FromObject(new { });

            // Get NLP Provider
            string providerName = Database.Configuration.GetSection($"{config}:Provider").Value;
            var provider = TypeHelper.GetInstance(providerName, Database.Assemblies) as INlpPipeline;
            provider.Configuration = Database.Configuration.GetSection("BotSharpAi");
            provider.Process("How are you today ?", data);


            // pipe process
            var pipelines = Database.Configuration.GetSection($"{config}:Pipe").Value
                .Split(',')
                .Select(x => x.Trim())
                .ToList();

            pipelines.ForEach(pipeName =>
            {
                var pipe = TypeHelper.GetInstance(pipeName, Database.Assemblies) as INlpPipeline;
                pipe.Configuration = provider.Configuration;
                var tokens = pipe.Process("How are you today ?", data);


            });


            return "";
        }
    }
}
