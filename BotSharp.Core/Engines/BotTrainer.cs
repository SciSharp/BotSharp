using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Core.Intents;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        public string Train(Agent agent)
        {
            agent.Intents = dc.Table<Intent>()
                .Include(x => x.Contexts)
                .Include(x => x.Responses).ThenInclude(x => x.Contexts)
                .Include(x => x.Responses).ThenInclude(x => x.Parameters).ThenInclude(x => x.Prompts)
                .Include(x => x.Responses).ThenInclude(x => x.Messages)
                .Include(x => x.UserSays).ThenInclude(x => x.Data)
                .Where(x => x.AgentId == agentId)
                .ToList();

            var data = JObject.FromObject(new
            {
            });

            // Get NLP Provider
            var config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
            var assemblies = (string[])AppDomain.CurrentDomain.GetData("Assemblies");
            var platform = config.GetSection($"BotPlatform").Value;
            string providerName = config.GetSection($"{platform}:Provider").Value;
            var provider = TypeHelper.GetInstance(providerName, assemblies) as INlpPipeline;
            provider.Configuration = config.GetSection(platform);
            provider.Process(agent, data);

            //var corpus = agent.GrabCorpus(dc);

            // pipe process
            var pipelines = provider.Configuration.GetSection($"Pipe").Value
                .Split(',')
                .Select(x => x.Trim())
                .ToList();

            pipelines.ForEach(pipeName =>
            {
                var pipe = TypeHelper.GetInstance(pipeName, assemblies) as INlpPipeline;
                pipe.Configuration = provider.Configuration;
                pipe.Process(agent, data);
            });


            return "";
        }
    }
}
