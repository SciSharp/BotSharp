using Bot.Rasa.Models;
using CustomEntityFoundation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.Rasa.Agents
{
    public static class AgentExtension
    {
        public static RasaTrainingData GrabCorpus(this RasaAgent agent, EntityDbContext dc)
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
