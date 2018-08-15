using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BotSharp.Core.Agents;
using BotSharp.Core.Entities;
using BotSharp.Core.Intents;
using BotSharp.Core.Models;
using DotNetToolkit;
using Newtonsoft.Json;

namespace BotSharp.Core.Engines.Rasa
{
    public class AgentImporterInRasa : IAgentImporter
    {
        public string AgentDir { get; set; }

        public Agent LoadAgent(AgentImportHeader agentHeader)
        {
            var agent = new Agent();
            agent.ClientAccessToken = Guid.NewGuid().ToString("N");
            agent.DeveloperAccessToken = Guid.NewGuid().ToString("N");
            agent.Id = agentHeader.Id;
            agent.Name = agentHeader.Name;

            return agent;
        }

        public void LoadBuildinEntities(Agent agent)
        {
            agent.Intents.ForEach(intent =>
            {
                if (intent.UserSays != null)
                {
                    intent.UserSays.ForEach(us =>
                    {
                        us.Data.Where(data => data.Meta != null)
                            .ToList()
                            .ForEach(data =>
                            {
                                LoadBuildinEntityTypePerUserSay(agent, data);
                            });
                    });
                }

            });
        }

        private void LoadBuildinEntityTypePerUserSay(Agent agent, IntentExpressionPart data)
        {
            var existedEntityType = agent.Entities.FirstOrDefault(x => x.Name == data.Meta);

            if (existedEntityType == null)
            {
                existedEntityType = new EntityType
                {
                    Name = data.Meta,
                    Entries = new List<EntityEntry>(),
                    IsOverridable = true
                };

                agent.Entities.Add(existedEntityType);
            }

            var entries = existedEntityType.Entries.Select(x => x.Value.ToLower()).ToList();
            if (!entries.Contains(data.Text.ToLower()))
            {
                existedEntityType.Entries.Add(new EntityEntry
                {
                    Value = data.Text,
                    Synonyms = new List<EntrySynonym>
                    {
                        new EntrySynonym
                        {
                            Synonym = data.Text
                        }
                    }
                });
            }
        }

        public void LoadCustomEntities(Agent agent)
        {
            agent.Entities = new List<EntityType>();
        }

        public void LoadIntents(Agent agent)
        {
            string data = File.ReadAllText(Path.Join(AgentDir, "corpus.json"));
            var rasa = JsonConvert.DeserializeObject<RasaAgent>(data);

            agent.Intents = rasa.UserSays.Select(x => x.Intent).Distinct().Select(x => new Intent { Name = x }).ToList();

            agent.Intents.ForEach(intent => {
                ImportIntentUserSays(intent, rasa.UserSays);
            });

        }

        private void ImportIntentUserSays(Intent intent, List<RasaIntentExpression> sentences)
        {
            intent.UserSays = new List<IntentExpression>();

            var userSays = sentences.Where(x => x.Intent == intent.Name).ToList();

            userSays.ForEach(say =>
            {
                var expression = new IntentExpression();

                say.Entities = say.Entities.OrderBy(x => x.Start).ToList();

                expression.Data = new List<IntentExpressionPart>();

                int pos = 0;
                for (int entityIdx = 0; entityIdx < say.Entities.Count; entityIdx++)
                {
                    var entity = say.Entities[entityIdx];

                    // previous
                    if (entity.Start > 0)
                    {
                        expression.Data.Add(new IntentExpressionPart
                        {
                            Text = say.Text.Substring(pos, entity.Start - pos)
                        });
                    }

                    // self
                    expression.Data.Add(new IntentExpressionPart
                    {
                        Alias = entity.Entity,
                        Meta = entity.Entity,
                        Text = say.Text.Substring(entity.Start, entity.Value.Length)
                    });

                    pos = entity.End + 1;

                    if (pos < say.Text.Length && entityIdx == say.Entities.Count - 1)
                    {
                        // end
                        expression.Data.Add(new IntentExpressionPart
                        {
                            Text = say.Text.Substring(pos)
                        });
                    }
                }

                if (say.Entities.Count == 0)
                {
                    expression.Data.Add(new IntentExpressionPart
                    {
                        Text = say.Text.Substring(pos)
                    });
                }

                int second = 0;
                expression.Data.ForEach(x => x.UpdatedTime = DateTime.UtcNow.AddSeconds(second++));

                intent.UserSays.Add(expression);
            });
        }

        public void AssembleTrainData(Agent agent)
        {
            // convert agent to training corpus
            agent.Corpus = new TrainingCorpus
            {
                Entities = new List<TrainingEntity>(),
                UserSays = new List<TrainingIntentExpression<TrainingIntentExpressionPart>>()
            };

            agent.Intents.ForEach(intent =>
            {
                intent.UserSays.ForEach(say => {
                    agent.Corpus.UserSays.Add(new TrainingIntentExpression<TrainingIntentExpressionPart>
                    {
                        Intent = intent.Name,
                        Text = String.Join("", say.Data.Select(x => x.Text)),
                        Entities = say.Data.Where(x => !String.IsNullOrEmpty(x.Meta))
                        .Select(x => new TrainingIntentExpressionPart
                        {
                            Value = x.Text,
                            Entity = x.Meta
                        })
                        .ToList()
                    });
                });
            });
        }
    }
}
