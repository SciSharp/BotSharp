using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Core.Intents;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyTextCategorizer : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }

        public bool Process(Agent agent, JObject data)
        {
            //var input = new List<Tuple<String, JObject>>();

            var texts = new List<String>();
            var golds = new List<JObject>();

            List<string> intentNames = agent.Intents.Select(x => x.Name).Distinct().ToList();

            agent.Intents.ForEach(intent =>
            {
                intent.UserSays.ForEach(userSay => {

                    var text = String.Join(string.Empty, userSay.Data.Select(say => say.Text));
                    var dim = JObject.FromObject(new { });

                    intentNames.ForEach(name =>
                    {
                        dim[name] = (intent.Name == name) ? 1 : 0;
                    });

                    //input.Add(new Tuple<string, JObject>(text, JObject.FromObject(new { Cats = dim })));
                    texts.Add(text);
                    golds.Add(JObject.FromObject(new { cats = dim }));
                });

            });

            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("textcategorizer", Method.POST);
            request.RequestFormat = DataFormat.Json;

            request.AddParameter("application/json", JsonConvert.SerializeObject(new { Texts = texts.Take(2), Golds = golds.Take(2), Labels = intentNames }), ParameterType.RequestBody);           

            var response = client.Execute<Result>(request);

            data["ModelName"] = response.Data.ModelName;
            
            //Predict
            var request2 = new RestRequest("predict", Method.GET);
            request2.AddParameter("text", "the roof is leaking");
            var response2 = client.Execute(request2);
            
            return true;
        }

        public class Result
        {
            public String ModelName { get; set; }
        }
    }
}
