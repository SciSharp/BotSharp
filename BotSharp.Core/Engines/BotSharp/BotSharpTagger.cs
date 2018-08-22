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

        public Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            string dataDir = Path.Combine(Configuration.GetValue<String>("BotSharpTagger:dataDir"), "CoNLL");
            var data = new CoNLLReader().Read(new ReaderOptions
            {
                DataDir = dataDir,
                FileName = "conll2000_chunking_train.txt"
            });

            var tagger = new TaggerFactory<NGramTagger>(new TagOptions
            {
                NGram = 1,
                Tag = "NN",
                Corpus = data
            }, SupportedLanguage.English);

            doc.Sentences.ForEach(x => tagger.Tag(new Sentence { Words = x.Tokens }));

            return true;
        }
    }
}
