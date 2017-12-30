using Bot.Rasa.Entities;
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

            var intents = dc.Intent().Include(x => x.Expressions).ToList();

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
