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
    public class AgentImporterInDialogflow : IAgentImporter
    {
        public Agent LoadAgent(string agentId, string agentDir)
        {
            // load agent profile
            string data = File.ReadAllText($"{agentDir}{Path.DirectorySeparatorChar}Dialogflow{Path.DirectorySeparatorChar}{agentId}{Path.DirectorySeparatorChar}agent.json");
            var agent = JsonConvert.DeserializeObject<DialogflowAgent>(data);
            agent.Id = Guid.NewGuid().ToString();
            agent.Name = agentId;

            return agent.ToObject<Agent>();
        }

        public void LoadEntities(Agent agent, string agentDir)
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
                        // void id confict
                        intent.Id = Guid.NewGuid().ToString();
                        intent.Name = intent.Name.Replace("/","_");
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

                                // remove @sys.ignore
                                intent.UserSays.ForEach(say =>
                                {
                                    say.Data.Where(x => x.Meta == "@sys.ignore").ToList().ForEach(x => x.Meta = null);
                                });
                            }
                        }


                        var newIntent = intent.ToObject<Intent>();
                        intent.Responses.ForEach(res =>
                        {
                            var newResponse = newIntent.Responses.First(x => x.Id == res.Id);

                            newResponse.Contexts = res.AffectedContexts.Select(x => new IntentResponseContext {
                                Name = x.Name,
                                Lifespan = x.Lifespan
                            }).ToList();

                            newResponse.Messages = res.MessageList.Where(x => x.Speech != null || x.Payload != null)
                                .Select(x =>
                                {
                                    if(x.Type == AIResponseMessageType.Custom)
                                    {
                                        return new IntentResponseMessage
                                        {
                                            Payload = JObject.FromObject(x.Payload),
                                            PayloadJson = JsonConvert.SerializeObject(x.Payload),
                                            Type = x.Type
                                        };
                                    } else
                                    {
                                        var speech = JsonConvert.SerializeObject(x.Speech.GetType().Equals(typeof(String)) ?
                                            new List<String> { x.Speech.ToString() } :
                                            (x.Speech as JArray).Select(s => s.Value<String>()).ToList());

                                        return new IntentResponseMessage
                                        {
                                            Speech = speech,
                                            Type = x.Type
                                        };
                                    }

                                }).ToList();

                            newResponse.Parameters = res.Parameters.Select(p =>
                            {
                                var rp = p.ToObject<IntentResponseParameter>();
                                rp.Prompts = p.PromptList.Select(pl => new ResponseParameterPrompt { Prompt = pl.Value }).ToList();
                                return rp;
                            }).ToList();
                        });
                        
                        newIntent.Contexts = intent.ContextList.Select(x => new IntentInputContext { Name = x }).ToList();

                        agent.Intents.Add(newIntent);
                    }
                });
        }
    }
}
