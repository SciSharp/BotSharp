using BotSharp.Core.Abstractions;
using CherubNLP;
using CherubNLP.Corpus;
using CherubNLP.Tag;
using CherubNLP.Tokenize;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.MachineLearning;
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

        public async Task<bool> Predict(AgentBase agent, NlpDoc doc, PipeModel meta)
        {
            Init();

            doc.Sentences.ForEach(x => _tagger.Tag(new Sentence
            {
                Words = x.Tokens,
                Text = x.Text
            }));

            return true;
        }

        public async Task<bool> Train(AgentBase agent, NlpDoc doc, PipeModel meta)
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
