using Bot.Rasa.Agents;
using CustomEntityFoundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bot.Rasa.Console
{
    public static class RequestExtension
    {
        public static AgentResponse TextRequest(this RasaConsole console, String agentId, String text)
        {
            var client = new RestClient($"{console.options.HostUrl}");

            var request = new RestRequest("parse?project={project}&q={text}", Method.GET);
            request.AddUrlSegment("project", agentId);
            request.AddUrlSegment("text", text);

            var response = client.Execute<AgentResponse>(request);

            return response.Data;
        }

        public static bool Train(this RasaConsole console, String agentId)
        {
            var client = new RestClient($"{console.options.HostUrl}");

            var request = new RestRequest("train", Method.POST);

            request.AddQueryParameter("project", agentId);
            
            string json = JsonConvert.SerializeObject(new
            {
                rasa_nlu_data = new RasaTrainingData
                {
                    UserSays = new List<UserSay>
                    {
                        new UserSay{ Text = "What's the weather like today?", Intent = "Weather" },
                        new UserSay{ Text = "Is gonna rain tomorrow?", Intent = "Weather"},
                        new UserSay{ Text = "Sunny", Intent = "Weather"},
                        new UserSay{ Text = "Is it raining outside?", Intent = "Weather"},
                        new UserSay{ Text = "It is raining", Intent = "Weather"},
                        new UserSay{ Text = "How old are you?", Intent = "Age"},
                        new UserSay{ Text = "When were you born?", Intent = "Age"},
                        new UserSay{ Text = "Where do you come from", Intent = "Country"},
                        new UserSay{ Text = "Where are you from?", Intent = "Country"},
                        new UserSay{ Text = "are you from US?", Intent = "Country"},
                        new UserSay{ Text = "What do you like for lunch?", Intent = "Lunch"},
                        new UserSay{ Text = "Would you like some cookie?", Intent = "Lunch"}
                    }
                }
            }, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

            request.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = client.Execute(request);

            return true;
        }

    }

    public class RasaTrainingData
    {
        [JsonProperty("common_examples")]
        public List<UserSay> UserSays { get; set; }
    }

    public class UserSay
    {
        public String Text { get; set; }
        public String Intent { get; set; }
    }
}
