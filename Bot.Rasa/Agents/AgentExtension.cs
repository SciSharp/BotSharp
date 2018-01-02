using Bot.Rasa.Entities;
using Bot.Rasa.Intents;
using Bot.Rasa.Models;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.Rasa.Agents
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

        public static String CreateEntity(this Agent agent, EntityType entity, Database dc)
        {
            return entity.Id;
        }

        public static RasaTrainingData GrabCorpus(this Agent agent, Database dc)
        {
            var trainingData = new RasaTrainingData
            {
                UserSays = new List<UserSay>()
            };

            var intents = dc.Table<Intent>().Include(x => x.Expressions).ToList();

            intents.ForEach(intent => {

                trainingData.UserSays.AddRange(intent.Expressions
                    .Select(exp => new UserSay
                    {
                        Intent = intent.Name,
                        Text = exp.Text
                    }));

            });

            return trainingData;
        }
    }
}
