using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.Intents;
using BotSharp.Platform.Rasa.Models;
using Newtonsoft.Json;

namespace BotSharp.Platform.Rasa
{
    public class AgentImporterInRasa<TAgent> : IAgentImporter<TAgent> where TAgent : AgentModel, new()
    {
        public string AgentDir { get; set; }

        public async Task<TAgent> LoadAgent(AgentImportHeader agentHeader)
        {
            var agent = new TAgent
            {
                Id = agentHeader.Id,
                Name = agentHeader.Name
            };

            return agent;
        }

        public async Task LoadBuildinEntities(TAgent agent)
        {
            agent.Intents.ForEach(intent =>
            {
                /*if (intent.UserSays != null)
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
                }*/
            });
        }

        private void LoadBuildinEntityTypePerUserSay(TAgent agent, IntentExpressionPart data)
        {
            /*var existedEntityType = agent.Entities.FirstOrDefault(x => x.Name == data.Meta);

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
            }*/
        }

        public async Task LoadCustomEntities(TAgent agent)
        {
        }

        public async Task LoadIntents(TAgent agent)
        {
            string data = File.ReadAllText(Path.Combine(AgentDir, "corpus.json"));
            var rasa = JsonConvert.DeserializeObject<RasaAgentImportModel>(data);

            agent.Intents = rasa.Data.Intents;
            agent.Entities = rasa.Data.Entities;
        }

        private void ImportIntentUserSays(RasaIntentExpression intent, List<RasaIntentExpression> sentences)
        {
            var intents = new List<RasaIntentExpression>();

            var userSays = sentences.Where(x => x.Intent == intent.Intent).ToList();

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
                            Text = say.Text.Substring(pos, entity.Start - pos),
                            Start = pos
                        });
                    }

                    // self
                    expression.Data.Add(new IntentExpressionPart
                    {
                        Alias = entity.Entity,
                        Meta = entity.Entity,
                        Text = say.Text.Substring(entity.Start, entity.Value.Length),
                        Start = entity.Start
                    });

                    pos = entity.End + 1;

                    if (pos < say.Text.Length && entityIdx == say.Entities.Count - 1)
                    {
                        // end
                        expression.Data.Add(new IntentExpressionPart
                        {
                            Text = say.Text.Substring(pos),
                            Start = pos
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

                intents.Add(say);
            });
        }

        /*public void AssembleTrainData(TAgent agent)
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
                            Entity = x.Meta,
                            Start = x.Start
                        })
                        .ToList()
                    });
                });
            });
        }*/
    }
}
