using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BotSharp.Core.Agents;
using BotSharp.Core.Intents;
using BotSharp.Core.Models;
using DotNetToolkit;
using Newtonsoft.Json;

namespace BotSharp.Core.Engines.Rasa
{
    public class AgentImporterInRasa : IAgentImporter
    {
        public Agent LoadAgent(AgentImportHeader agentHeader, string agentDir)
        {
            var agent = new Agent();
            agent.ClientAccessToken = Guid.NewGuid().ToString("N");
            agent.DeveloperAccessToken = Guid.NewGuid().ToString("N");
            agent.Id = agentHeader.Id;
            agent.Name = agentHeader.Name;

            return agent;
        }

        public void LoadBuildinEntities(Agent agent, string agentDir)
        {
            
        }

        public void LoadCustomEntities(Agent agent, string agentDir)
        {
            
        }

        public void LoadIntents(Agent agent, string agentDir)
        {
            string data = File.ReadAllText(Path.Join(agentDir, "corpus.json"));
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
    }
}
