using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BotSharp.Core.Adapters.Dialogflow;
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
    /// Import agent from Dialogflow
    /// </summary>
    public class AgentImporterInDialogflow : IAgentImporter
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
            string data = File.ReadAllText($"{agentDir}{Path.DirectorySeparatorChar}Dialogflow{Path.DirectorySeparatorChar}{agentHeader.Name}{Path.DirectorySeparatorChar}agent.json");
            var agent = JsonConvert.DeserializeObject<DialogflowAgent>(data);
            agent.Name = agentHeader.Name;
            agent.Id = agentHeader.Id;

            var result = agent.ToObject<Agent>();
            result.ClientAccessToken = agentHeader.ClientAccessToken;
            result.DeveloperAccessToken = agentHeader.DeveloperAccessToken;
            if(agentHeader.UserId != null)
            {
                result.UserId = agentHeader.UserId;
            }

            result.MlConfig = agent.ToObject<AgentMlConfig>();
            result.MlConfig.MinConfidence = agent.MlMinConfidence;
            result.MlConfig.AgentId = agent.Id;

            return result;
        }

        public void LoadCustomEntities(Agent agent, string agentDir)
        {
            agent.Entities = new List<EntityType>();
            string entityDir = $"{agentDir}{Path.DirectorySeparatorChar}Dialogflow{Path.DirectorySeparatorChar}{agent.Name}{Path.DirectorySeparatorChar}entities";
            if (!Directory.Exists(entityDir)) return;

            Directory.EnumerateFiles(entityDir)
                .ToList()
                .ForEach(fileName =>
                {
                    string entityName = fileName.Split($"{Path.DirectorySeparatorChar}").Last();
                    if (!entityName.Contains("_"))
                    {
                        string entityJson = File.ReadAllText($"{fileName}");
                        var entity = JsonConvert.DeserializeObject<DialogflowEntity>(entityJson);

                        // load entries
                        string entriesFileName = fileName.Replace(entity.Name, $"{entity.Name}_entries_{agent.Language}");
                        if (File.Exists(entriesFileName))
                        {
                            string entriesJson = File.ReadAllText($"{entriesFileName}");
                            entriesJson = entriesJson.Replace("\"synonyms\":", "\"rawSynonyms\":");
                            entity.Entries = JsonConvert.DeserializeObject<List<DialogflowEntityEntry>>(entriesJson);
                            entity.Entries.ForEach(x => x.Synonyms = x.RawSynonyms.Select(s => new EntrySynonym
                            {
                                Synonym = s
                            }).ToList());
                        }

                        var entityType = entity.ToObject<EntityType>();
                        agent.Entities.Add(entityType);
                    }
                });
        }

        public void LoadIntents(Agent agent, string agentDir)
        {
            agent.Intents = new List<Intent>();
            string intentDir = $"{agentDir}{Path.DirectorySeparatorChar}Dialogflow{Path.DirectorySeparatorChar}{agent.Name}{Path.DirectorySeparatorChar}intents";
            if (!Directory.Exists(intentDir)) return;

            Directory.EnumerateFiles(intentDir)
                .ToList()
                .ForEach(fileName =>
                {
                    if (!fileName.Contains("_usersays_" + agent.Language)
                        || fileName.Contains("Default Fallback Intent"))
                    {
                        string intentJson = File.ReadAllText($"{fileName}");

                        // avoid confict data structure
                        intentJson = intentJson.Replace("\"contexts\":", "\"contextList\":");
                        intentJson = intentJson.Replace("\"messages\":", "\"messageList\":");
                        intentJson = intentJson.Replace("\"prompts\":", "\"promptList\":");

                        var intent = JsonConvert.DeserializeObject<DialogflowIntent>(intentJson);
                        var newIntent = ImportIntentUserSays(agent, intent, fileName);
                        agent.Intents.Add(newIntent);
                    }
                });
        }

        private Intent ImportIntentUserSays(Agent agent, DialogflowIntent intent, string fileName)
        {
            // void id confict
            intent.Id = Guid.NewGuid().ToString();
            intent.Name = intent.Name.Replace("/", "_");
            // load user expressions
            if (fileName.Contains("Default Fallback Intent"))
            {
                intent.UserSays = (intent.Responses[0].MessageList[0].Speech as JArray)
                .Select(x => new DialogflowIntentExpression
                {
                    Data = new List<DialogflowIntentExpressionPart>
                    {
                        new DialogflowIntentExpressionPart
                        {
                            Text = x.ToString()
                        }
                    }
                }).ToList();
            }
            else
            {
                string expressionFileName = fileName.Replace(intent.Name, $"{intent.Name}_usersays_{agent.Language}");
                if (File.Exists(expressionFileName))
                {
                    string expressionJson = File.ReadAllText($"{expressionFileName}");
                    intent.UserSays = JsonConvert.DeserializeObject<List<DialogflowIntentExpression>>(expressionJson);

                    intent.UserSays.ForEach(say =>
                    {
                        // remove @sys.ignore
                        say.Data.Where(x => x.Meta == "@sys.ignore").ToList().ForEach(x => x.Meta = null);

                        // remove @sys.
                        say.Data.Where(x => x.Meta != null && x.Meta.StartsWith("@sys.")).ToList().ForEach(x => x.Meta = x.Meta.Substring(5));

                        // remove @
                        say.Data.Where(x => x.Meta != null && x.Meta.StartsWith("@")).ToList().ForEach(x => x.Meta = x.Meta.Substring(1));
                    });
                }
            }

            var newIntent = ImportIntentResponse(agent, intent);

            newIntent.Contexts = intent.ContextList.Select(x => new IntentInputContext { Name = x }).ToList();

            return newIntent;
        }

        private Intent ImportIntentResponse(Agent agent, DialogflowIntent intent)
        {
            var newIntent = intent.ToObject<Intent>();

            intent.Responses.ForEach(res =>
            {
                var newResponse = newIntent.Responses.First(x => x.Id == res.Id);

                newResponse.Contexts = res.AffectedContexts.Select(x => new IntentResponseContext
                {
                    Name = x.Name,
                    Lifespan = x.Lifespan
                }).ToList();

                int millSeconds = 0;

                newResponse.Messages = res.MessageList.Where(x => x.Speech != null || x.Payload != null)
                    .Select(x =>
                    {
                        if (x.Type == AIResponseMessageType.Custom)
                        {
                            return new IntentResponseMessage
                            {
                                Payload = JObject.FromObject(x.Payload),
                                PayloadJson = JsonConvert.SerializeObject(x.Payload),
                                Type = x.Type,
                                UpdatedTime = DateTime.UtcNow.AddMilliseconds(millSeconds++)
                            };
                        }
                        else
                        {
                            var speech = JsonConvert.SerializeObject(x.Speech.GetType().Equals(typeof(String)) ?
                                new List<String> { x.Speech.ToString() } :
                                (x.Speech as JArray).Select(s => s.Value<String>()).ToList());

                            return new IntentResponseMessage
                            {
                                Speech = speech,
                                Type = x.Type,
                                UpdatedTime = DateTime.UtcNow.AddMilliseconds(millSeconds++)
                            };
                        }

                    }).ToList();

                newResponse.Parameters = res.Parameters.Select(p =>
                {
                    var rp = p.ToObject<IntentResponseParameter>();

                    // remove @sys.
                    if (rp.DataType.StartsWith("@sys."))
                    {
                        rp.DataType = rp.DataType.Substring(5);
                    }

                    if (rp.DataType.StartsWith("@"))
                    {
                        rp.DataType = rp.DataType.Substring(1);
                    }

                    rp.Prompts = p.PromptList.Select(pl => new ResponseParameterPrompt { Prompt = pl.Value }).ToList();
                    return rp;
                }).ToList();
            });

            return newIntent;
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
}
