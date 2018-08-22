using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
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

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            return true;
        }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            List<List<Token>> tokens = new List<List<Token>>();
            var corpus = agent.Corpus;

            var tokenizer = new TokenizerFactory<RegexTokenizer>(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WORD_PUNC
            }, SupportedLanguage.English);

            doc.Sentences = new List<NlpDocSentence>();
            List<string> sentencesList = new List<string>();
            corpus.UserSays.ForEach(say =>
            {
                doc.Sentences.Add(new NlpDocSentence
                {
                    Tokens = tokenizer.Tokenize(say.Text),
                    Text = say.Text
                });
            });

            return true;
        }
    }
}
