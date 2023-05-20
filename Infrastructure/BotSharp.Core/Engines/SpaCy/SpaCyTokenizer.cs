using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Core.Models;
using BotSharp.NLP.Tokenize;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.SpaCy
{
    public class SpaCyTokenizer : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("tokenizer", Method.POST);
            List<List<Token>> tokens = new List<List<Token>>();
            Boolean res = true;
            var corpus = agent.Corpus;

            doc.Sentences = new List<NlpDocSentence>();
            List<string> sentencesList = new List<string>();
            corpus.UserSays.ForEach(usersay => sentencesList.Add(usersay.Text));

            request.RequestFormat = DataFormat.Json;
            request.Method = Method.POST;
            request.AddHeader("Content-Type", "application/json; charset=utf-8");
            request.AddParameter("application/json", JsonConvert.SerializeObject(new Documents(sentencesList)), ParameterType.RequestBody);

            var response = client.Execute<Result>(request);

            tokens = response.Data.TokensList;

            for (int i = 0; i < sentencesList.Count; i++)
            {
                doc.Sentences.Add(new NlpDocSentence
                {
                    Tokens = tokens[i],
                    Text = sentencesList[i]
                });
            }
            res = res && response.IsSuccessful;
            return res;
        }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            var client = new RestClient(Configuration.GetSection("SpaCyProvider:Url").Value);
            var request = new RestRequest("tokenizer", Method.GET);
            List<List<Token>> tokens = new List<List<Token>>();
            Boolean res = true;
            var corpus = agent.Corpus;

            request.AddParameter("text", doc.Sentences[0].Text);
            var response = client.Execute<Result>(request);
            
            tokens = response.Data.TokensList;

            res = res && response.IsSuccessful;

            doc.Sentences[0].Tokens = tokens[0];

            return true;
        }

        private class Result
        {
            public List<List<Token>> TokensList { get; set; }
        }

        private class Documents
        {
            public List<string> Sentences { get; set; }

            public Documents(List<string> sentences)
            {
                this.Sentences = sentences;
            }
        }
    }
}
