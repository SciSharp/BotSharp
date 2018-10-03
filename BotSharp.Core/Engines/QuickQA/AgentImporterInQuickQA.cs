using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BotSharp.Core.Agents;
using BotSharp.Core.Intents;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using Newtonsoft.Json;

namespace BotSharp.Core.Engines.QuickQA
{
    public class AgentImporterInQuickQA<TAgent> : IAgentImporter<TAgent>
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
            
        }

        public void LoadCustomEntities(Agent agent)
        {
            
        }

        public void LoadIntents(Agent agent)
        {
            string lines = File.ReadAllText(Path.Combine(AgentDir, "corpus.txt"));

            var questions = Regex.Matches(lines, @"^Q - .+\n", RegexOptions.Multiline).Cast<Match>().ToArray();
            var answers = Regex.Matches(lines, @"^A - ", RegexOptions.Multiline).Cast<Match>().ToArray();

            int qNumber = 1;
            agent.Intents = questions.Select(x => new Intent
            {
                Name = $"Q{qNumber++}",
                UserSays = new List<IntentExpression>
                {
                    new IntentExpression
                    {
                        Data = new List<IntentExpressionPart>
                        {
                            new IntentExpressionPart
                            {
                                Text = x.Value.Substring(4).Trim()
                            }
                        }
                    }
                }
            }).ToList();

            // assemble answers
            for (int idx = 0; idx < agent.Intents.Count(); idx++)
            {
                var intent = agent.Intents[idx];
                var answer = answers[idx];

                var start = answer.Index + 4;
                var length = ((idx == agent.Intents.Count() - 1) ? lines.Length : questions[idx + 1].Index) - answer.Index - 4;

                intent.Responses = new List<IntentResponse>
                {
                    new IntentResponse
                    {
                        Messages = new List<IntentResponseMessage>
                        {
                            new IntentResponseMessage
                            {
                                Speech = lines.Substring(start, length).Trim()
                            }
                        }
                    }
                };
            }
        }

        public void AssembleTrainData(Agent agent)
        {

        }
    }
}
