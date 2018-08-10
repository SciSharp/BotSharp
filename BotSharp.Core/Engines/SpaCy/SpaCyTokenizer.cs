using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Core.Models;
using BotSharp.MachineLearning.NLP;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyTokenizer : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("tokenizer", Method.GET);
            List<List<NlpToken>> tokens = new List<List<NlpToken>>();
            Boolean res = true;
            var dc = new DefaultDataContextLoader().GetDefaultDc();
            var corpus = agent.Corpus;

            doc.Sentences = new List<NlpDocSentence>();

            corpus.UserSays.ForEach(usersay => {
                Console.WriteLine(usersay.Text);
                request.AddParameter("text", usersay.Text);
                var response = client.Execute<Result>(request);
                
                tokens.Add(response.Data.Tokens);

                doc.Sentences.Add(new NlpDocSentence
                {
                    Tokens = response.Data.Tokens,
                    Text = usersay.Text
                });

                res = res && response.IsSuccessful;
                
            });

            return res;
        }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("tokenizer", Method.GET);
            List<List<NlpToken>> tokens = new List<List<NlpToken>>();
            Boolean res = true;
            var corpus = agent.Corpus;

            request.AddParameter("text", doc.Sentences[0].Text);
            var response = client.Execute<Result>(request);
            
            tokens.Add(response.Data.Tokens);

            res = res && response.IsSuccessful;

            doc.Sentences[0].Tokens = tokens[0];

            return true;
        }

        private class Result
        {
            public List<NlpToken> Tokens { get; set; }
        }
    }
}
