using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotSharp.NLP.Corpus
{
    /// <summary>
    /// A corpus reader for CoNLL-style files.  These files consist of a
    /// series of sentences, separated by blank lines.Each sentence is
    /// encoded using a table(or "grid") of values, where each line
    /// corresponds to a single word, and each column corresponds to an
    /// annotation type.The set of columns used by CoNLL-style files can
    /// vary from corpus to corpus;
    /// </summary>
    public class CoNLLReader
    {
        public List<Sentence> Read(ReaderOptions options)
        {
            string corpus = File.ReadAllText(Path.Combine(options.DataDir, "conll2000_chunking_train.txt"));

            return null;
        }
    }
}
