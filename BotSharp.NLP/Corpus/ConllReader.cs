using BotSharp.NLP.Tokenize;
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
            var sentences = new List<Sentence>();
            using(StreamReader reader = new StreamReader(Path.Combine(options.DataDir, options.FileName)))
            {
                string line = reader.ReadLine();
                var sentence = new Sentence { Words = new List<Token> { } };

                while (!reader.EndOfStream)
                {
                    if (String.IsNullOrEmpty(line))
                    {
                        sentences.Add(sentence);
                        sentence = new Sentence { Words = new List<Token> { } };
                    }
                    else
                    {
                        var columns = line.Split(' ');

                        sentence.Words.Add(new Token
                        {
                            Text = columns[0],
                            Pos = columns[1]
                        });
                    }

                    line = reader.ReadLine();
                }
                
            }

            return sentences;
        }
    }
}
