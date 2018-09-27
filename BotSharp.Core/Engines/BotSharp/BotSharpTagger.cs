using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.NLP;
using BotSharp.NLP.Corpus;
using BotSharp.NLP.Tag;
using BotSharp.NLP.Tokenize;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.BotSharp
{
    public class BotSharpTagger : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        private TaggerFactory _tagger;

        public BotSharpTagger()
        {

        }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            Init();

            doc.Sentences.ForEach(x => _tagger.Tag(new Sentence
            {
                Words = x.Tokens,
                Text = x.Text
            }));

            return true;
        }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            Init();

            doc.Sentences.ForEach(x => _tagger.Tag(new Sentence
            {
                Words = x.Tokens,
                Text = x.Text
            }));

            return true;
        }

        private void Init()
        {
            if (_tagger == null)
            {
                _tagger = new TaggerFactory(new TagOptions
                {
                    CorpusDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Corpus")
                }, SupportedLanguage.English);

                string tokenizerName = Configuration.GetValue<String>($"tagger");

                _tagger.GetTagger(tokenizerName);
            }
        }
    }
}
