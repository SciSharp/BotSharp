using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.NLP;
using BotSharp.NLP.Corpus;
using BotSharp.NLP.Tag;
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

        private TaggerFactory<NGramTagger> _tagger;

        public BotSharpTagger()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Corpus", "CoNLL");
            var data = new CoNLLReader().Read(new ReaderOptions
            {
                DataDir = dataDir,
                FileName = "conll2000_chunking_train.txt"
            });

            _tagger = new TaggerFactory<NGramTagger>(new TagOptions
            {
                NGram = 1,
                Tag = "NN",
                Corpus = data
            }, SupportedLanguage.English);
        }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            doc.Sentences.ForEach(x => _tagger.Tag(new Sentence { Words = x.Tokens }));

            return true;
        }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            doc.Sentences.ForEach(x => _tagger.Tag(new Sentence { Words = x.Tokens }));

            return true;
        }
    }
}
