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

        public static String CreateEntity(this Agent agent, Entity entity, Database dc)
        {
            return entity.Id;
        }

        public static RasaTrainingData GrabCorpus(this Agent agent, Database dc, List<AIContext> ctx)
        {
            var trainingData = new RasaTrainingData
            {
                UserSays = new List<UserSay>()
            };

            var intents = dc.Table<Intent>()
                .Include(x => x.Contexts)
                .Include(x => x.UserSays).ThenInclude(say => say.Data).ToList();

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
                trainingData.UserSays.AddRange(intent.UserSays
                    .Select(exp => new UserSay
                    {
                        Intent = intent.Name,
                        Text = String.Join("", exp.Data.OrderBy(x => x.UpdatedTime).Select(x => x.Text))
                    }));
            });

            return trainingData;
        }
    }
}
