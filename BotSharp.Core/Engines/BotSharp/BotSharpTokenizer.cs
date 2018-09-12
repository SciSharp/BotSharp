using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Core.Intents;
using BotSharp.NLP;
using BotSharp.NLP.Tokenize;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.BotSharp
{
    public class BotSharpTokenizer : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }
        private TokenizerFactory<TreebankTokenizer> _tokenizer;

        public BotSharpTokenizer()
        {
            _tokenizer = new TokenizerFactory<TreebankTokenizer>(new TokenizationOptions
            {
            }, SupportedLanguage.English);
        }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            doc.Tokenizer = this;

            // same as train
            doc.Sentences.ForEach(snt =>
            {
                snt.Tokens = _tokenizer.Tokenize(snt.Text);
            });

            return true;
        }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            doc.Tokenizer = this;
            doc.Sentences = new List<NlpDocSentence>();

            agent.Corpus.UserSays.ForEach(say =>
            {
                doc.Sentences.Add(new NlpDocSentence
                {
                    Tokens = _tokenizer.Tokenize(say.Text),
                    Text = say.Text,
                    Intent = new TextClassificationResult { Label = say.Intent }
                });
            });

            return true;
        }
    }
}
