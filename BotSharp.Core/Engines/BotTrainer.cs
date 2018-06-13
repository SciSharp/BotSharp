using System;
using System.Collections.Generic;
using System.Text;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;

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
            // Get NLP Provider
            string providerName = Database.Configuration.GetSection($"{config}:Provider").Value;
            var provider = TypeHelper.GetInstance(providerName, Database.Assemblies) as INlpProvider;


            
            // tokenize
            ITokenizer tokenizer;

            INlpProvider nlpProvider;

            return "";
        }
    }
}
