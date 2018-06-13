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
            provider.Configuration = Database.Configuration.GetSection("BotSharpAi");

            provider.Load();

            // pipe process

            // Tokenize
            var tokenizerName = provider.Configuration.GetSection("Pipe:Tokenizer").Value;
            var tokenizer = TypeHelper.GetInstance(tokenizerName, Database.Assemblies) as INlpTokenizer;
            tokenizer.Configuration = provider.Configuration;
            var tokens = tokenizer.Tokenize("How are you doing?");

            // Featurize
            var featurizerName = provider.Configuration.GetSection("Pipe:Featurizer").Value;
            var featurizer = TypeHelper.GetInstance(featurizerName, Database.Assemblies) as INlpFeaturizer;
            featurizer.Configuration = provider.Configuration;
            var features = featurizer.Featurize("How are you doing?");

            // Entitize
            var entitizerName = provider.Configuration.GetSection("Pipe:Entitizer").Value;
            var entitizer = TypeHelper.GetInstance(entitizerName, Database.Assemblies) as INlpEntitizer;
            entitizer.Configuration = provider.Configuration;
            var entities = entitizer.Entitize("Where are you going tomorrow ?");

            return "";
        }
    }
}
