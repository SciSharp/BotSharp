using BotSharp.Core.Adapters.Rasa;
using BotSharp.Core.Engines;
using BotSharp.Core.Entities;
using BotSharp.Core.Intents;
using BotSharp.Core.Models;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Agents
{
    public static class AgentDriver
    {
        public static Agent LoadAgentById(this IBotPlatform engine, Database dc, string agentId)
        {
            var clientAccessToken = dc.Table<Agent>().Find(agentId).ClientAccessToken;

            var config = new AIConfiguration(clientAccessToken, SupportedLanguage.English);

            var rasa = new RasaAi(dc, config);
            rasa.agent = rasa.LoadAgent(dc, config);

            return rasa.agent;
        }

        public static Agent LoadAgent(this IBotPlatform engine, Database dc, AIConfiguration aiConfig)
        {
            return dc.Table<Agent>()
                .Include(x => x.Intents).ThenInclude(x => x.Contexts)
                .Include(x => x.Entities).ThenInclude(x => x.Entries).ThenInclude(x => x.Synonyms)
                .FirstOrDefault(x => x.ClientAccessToken == aiConfig.ClientAccessToken || x.DeveloperAccessToken == aiConfig.ClientAccessToken);
        }

        /// <summary>
        /// Restore a agent instance from backup json files
        /// </summary>
        /// <param name="importor"></param>
        /// <param name="agentId"></param>
        /// <returns></returns>
        public static Agent RestoreAgent(this IBotPlatform engine, IAgentImporter importer, String agentId, string dataDir)
        {
            // Load agent summary
            var agent = importer.LoadAgent(agentId, dataDir);

            // Load agent entities
            importer.LoadEntities(agent, dataDir);

            // Load agent intents
            importer.LoadIntents(agent, dataDir);

            return agent;
        }

        public static String SaveAgent(this Agent agent, Database dc)
        {
            var existedAgent = dc.Table<Agent>().FirstOrDefault(x => x.Id == agent.Id || x.Name == agent.Name);
            if (existedAgent == null)
            {
                dc.Table<Agent>().Add(agent);
                return agent.Id;
            }
            else
            {
                agent.Id = existedAgent.Id;
                return existedAgent.Id;
            }
        }

        public static RasaTrainingData GrabCorpus(this Agent agent, Database dc)
        {
            var trainingData = new RasaTrainingData
            {
                Entities = new List<RasaTraningEntity>(),
                UserSays = new List<RasaIntentExpression>()
            };

            var expressParts = new List<IntentExpressionPart>();

            var intents = dc.Table<Intent>()
                .Include(x => x.Contexts)
                .Include(x => x.UserSays).ThenInclude(say => say.Data)
                .Where(x => x.AgentId == agent.Id && x.UserSays.Count > 0)
                .ToList();

            intents.ForEach(intent =>
            {
                intent.UserSays.ForEach(exp =>
                {
                    var say = new RasaIntentExpression
                    {
                        Intent = intent.Name,
                        Text = String.Join("", exp.Data.OrderBy(x => x.UpdatedTime).Select(x => x.Text)),
                        ContextHash = intent.ContextHash
                    };

                    // convert entity format
                    exp.Data.Where(x => !String.IsNullOrEmpty(x.Meta))
                    .ToList()
                    .ForEach(x =>
                    {
                        int start = say.Text.IndexOf(x.Text);

                        var part = new RasaIntentExpressionPart
                        {
                            Value = x.Text,
                            Entity = x.Alias,
                            Start = start,
                            End = start + x.Text.Length
                        };

                        if (say.Entities == null) say.Entities = new List<RasaIntentExpressionPart>();
                        say.Entities.Add(part);

                        // assemble entity synonmus
                        if (!trainingData.Entities.Any(y => y.EntityType == x.Alias && y.EntityValue == x.Text))
                        {
                            var allSynonyms = (from e in dc.Table<EntityType>()
                                               join ee in dc.Table<EntityEntry>() on e.Id equals ee.EntityId
                                               join ees in dc.Table<EntrySynonym>() on ee.Id equals ees.EntityEntryId
                                               where e.Name == x.Alias && ee.Value == x.Text & ees.Synonym != x.Text
                                               select ees.Synonym).ToList();

                            var te = new RasaTraningEntity
                            {
                                EntityType = x.Alias,
                                EntityValue = x.Text,
                                Synonyms = allSynonyms
                            };

                            trainingData.Entities.Add(te);
                        }
                    });

                    trainingData.UserSays.Add(say);
                });
            });

            // remove Default Fallback Intent
            trainingData.UserSays = trainingData.UserSays.Where(x => x.Intent != "Default Fallback Intent").ToList();

            return trainingData;
        }
    }
}
