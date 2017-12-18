using Bot.Rasa;
using Bot.Rasa.Agents;
using Bot.Rasa.Intents;
using CustomEntityFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.UnitTest
{
    public class GenerateTestData
    {
        public void LoadData(EntityDbContext dc, RasaAgent agent)
        {
            var intent = new RasaIntent
            {
                AgentId = agent.Id,
                Name = "Weather",
                Expressions = new List<RasaIntentExpression>
                {
                    new RasaIntentExpression { Text ="What is the weather like today in Chicago?" },
                    new RasaIntentExpression { Text ="Is it will be rain?" },
                    new RasaIntentExpression { Text ="It's windy outside?" },
                    new RasaIntentExpression { Text ="It's very code there?" }
                }
            };

            if (dc.Intent().Any(x => x.Name == intent.Name)) return;

            dc.Intent().Add(intent);
        }
    }
}
