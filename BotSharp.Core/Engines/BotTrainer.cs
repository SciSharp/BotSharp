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
            string providerName = Database.Configuration.GetSection($"{config}:Provider").Value;
            var provider = TypeHelper.GetInstance(providerName, Database.Assemblies) as INlpPipeline;
            provider.Configuration = Database.Configuration.GetSection("BotSharpAi");
            provider.Process(agent, data);

            //var corpus = agent.GrabCorpus(dc);

            // pipe process
            var pipelines = Database.Configuration.GetSection($"{config}:Pipe").Value
                .Split(',')
                .Select(x => x.Trim())
                .ToList();

            pipelines.ForEach(pipeName =>
            {
                var pipe = TypeHelper.GetInstance(pipeName, Database.Assemblies) as INlpPipeline;
                pipe.Configuration = provider.Configuration;
                pipe.Process(agent, data);
            });


            return "";
        }
    }
}
