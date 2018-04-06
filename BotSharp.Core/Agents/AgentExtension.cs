using BotSharp.Core.Adapters.Rasa;
using BotSharp.Core.Entities;
using BotSharp.Core.Expressions;
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
    public static class AgentExtension
    {
        /// <summary>
        /// Get agent header row from Agent table
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="agentId"></param>
        /// <returns></returns>
        public static Agent Agent(this Database dc, string agentId)
        {
            return dc.Table<Agent>().Find(agentId);
        }

        public static String CreateEntity(this Agent agent, Entity entity, Database dc)
        {
            return entity.Id;
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
                .Where(x => x.UserSays.Count > 0)
                .ToList();

            intents.ForEach(intent =>
            {
                intent.UserSays.ForEach(exp =>
                {
                    var say = new RasaIntentExpression
                    {
                        Intent = intent.Name,
                        Text = String.Join("", exp.Data.OrderBy(x => x.UpdatedTime).Select(x => x.Text)),
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
                            var allSynonyms = (from e in dc.Table<Entity>()
                                               join ee in dc.Table<EntityEntry>() on e.Id equals ee.EntityId
                                               join ees in dc.Table<EntityEntrySynonym>() on ee.Id equals ees.EntityEntryId
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

            return trainingData;
        }

        public static RasaTrainingData GrabCorpusPerContexts(this Agent agent, Database dc, List<AIContext> ctx)
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
                .Where(x => x.UserSays.Count > 0)
                .ToList();

            var contexts = ctx.OrderBy(x => x.Name).Select(x => x.Name.ToLower()).ToList();

            // search all potential intents which input context included in contexts
            intents = intents.Where(it =>
            {
                if (contexts.Count == 0)
                {
                    return it.Contexts.Count() == 0;
                }
                else
                {
                    return it.Contexts.Count() > 0 && it.Contexts.Count(x => contexts.Contains(x.Name.ToLower())) == it.Contexts.Count;
                }
            }).OrderByDescending(x => x.Contexts.Count).ToList();

            intents.ForEach(intent =>
            {
                intent.UserSays.ForEach(exp =>
                {
                    var say = new RasaIntentExpression
                    {
                        Intent = intent.Name,
                        Text = String.Join("", exp.Data.OrderBy(x => x.UpdatedTime).Select(x => x.Text)),
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
                            var allSynonyms = (from e in dc.Table<Entity>()
                                              join ee in dc.Table<EntityEntry>() on e.Id equals ee.EntityId
                                              join ees in dc.Table<EntityEntrySynonym>() on ee.Id equals ees.EntityEntryId
                                              where e.Name == x.Alias && ee.Value == x.Text & ees.Synonym != x.Text
                                               select ees.Synonym ).ToList();

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

            

            return trainingData;
        }
    }
}
