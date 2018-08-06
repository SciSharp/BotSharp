using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BotSharp.Core.Adapters.Dialogflow;
using BotSharp.Core.Adapters.Sebis;
using BotSharp.Core.Agents;
using BotSharp.Core.Entities;
using BotSharp.Core.Intents;
using BotSharp.Core.Models;
using DotNetToolkit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BotSharp.Core.Engines
{
    /// <summary>
    /// Import Sebis NLU benchmark corpus
    /// </summary>
    public class AgentImporterInSebis : IAgentImporter
    {
        /// <summary>
        /// Load agent meta
        /// </summary>
        /// <param name="agentName"></param>
        /// <param name="agentDir"></param>
        /// <returns></returns>
        public Agent LoadAgent(AgentImportHeader agentHeader, string agentDir)
        {
            // load agent profile
            string data = File.ReadAllText(Path.Join(agentDir, "Sebis", $"{agentHeader.Name}{Path.DirectorySeparatorChar}agent.json"));
            var agent = JsonConvert.DeserializeObject<SebisAgent>(data);
            agent.Name = agentHeader.Name;
            agent.Id = agentHeader.Id;

            var result = agent.ToObject<Agent>();
            result.ClientAccessToken = agentHeader.ClientAccessToken;
            result.DeveloperAccessToken = agentHeader.DeveloperAccessToken;
            if(agentHeader.UserId != null)
            {
                result.UserId = agentHeader.UserId;
            }

            return result;
        }

        public void LoadCustomEntities(Agent agent, string agentDir)
        {
            agent.Entities = new List<EntityType>();
        }

        public void LoadIntents(Agent agent, string agentDir)
        {
            string data = File.ReadAllText(Path.Join(agentDir, "Sebis", $"{agent.Name}{Path.DirectorySeparatorChar}corpus.json"));
            var sentences = JsonConvert.DeserializeObject<SebisAgent>(data).Sentences;
            
            agent.Intents = sentences.Select(x => x.Name).Distinct().Select(x => new Intent{Name = x}).ToList();
            
            agent.Intents.ForEach(intent => {
                ImportIntentUserSays(intent, sentences);
            });
        }

        private void ImportIntentUserSays(Intent intent, List<SebisIntent> sentences)
        {
            intent.UserSays = new List<IntentExpression>();

            var userSays = sentences.Where(x => x.Name == intent.Name).ToList();

            userSays.ForEach(say =>
            {
                var expression = new IntentExpression();

                say.Entities = say.Entities.OrderBy(x => x.Start).ToList();

                int pos = 0;
                say.Entities.ForEach(x =>
                {
                    ConvertWordPosToCharPos(say.Text, x, pos);
                    pos = x.End + 1;
                });

                expression.Data = new List<IntentExpressionPart>();

                pos = 0;
                for (int entityIdx = 0; entityIdx < say.Entities.Count; entityIdx++)
                {
                    var entity = say.Entities[entityIdx];

                    // previous
                    if(entity.Start > 0)
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

                    pos = entity.End;

                    if (pos < say.Text.Length && entityIdx == say.Entities.Count - 1)
                    {
                        // end
                        expression.Data.Add(new IntentExpressionPart
                        {
                            Text = say.Text.Substring(pos)
                        });
                    }
                }

<<<<<<< HEAD
=======
                int second = 0;
                expression.Data.ForEach(x => x.UpdatedTime = DateTime.UtcNow.AddSeconds(second++));

>>>>>>> b389d837f25afa256b35d0418274967557fb06f9
                intent.UserSays.Add(expression);
            });
        }

        private void ConvertWordPosToCharPos(string text, SebisIntentExpressionPart entity, int start)
        {
            entity.Start = text.IndexOf(entity.Value, start);
            entity.End = entity.Start + entity.Value.Length - 1;
        }

        public void LoadBuildinEntities(Agent agent, string agentDir)
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
    }

    public class Sebis
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public string Lang { get; set; }
        public List<TrainingIntentExpression<TrainingIntentExpressionPart>> Sentences { get; set; }
    }


}
