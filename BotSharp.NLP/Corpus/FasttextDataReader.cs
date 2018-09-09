using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BotSharp.NLP.Corpus
{
    /// <summary>
    /// Fasttext labeled data reader
    /// </summary>
    public class FasttextDataReader
    {
        public List<Sentence> Read(ReaderOptions options)
        {
            var sentences = new List<Sentence>();
            using (StreamReader reader = new StreamReader(Path.Combine(options.DataDir, options.FileName)))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        var ms = Regex.Matches(line, @"__label__\w+\s").Cast<Match>().ToList();

                        sentences.Add(new Sentence
                        {
                            // Label = lable,
                            Text = line
                        });
                    }
                }
            }

            return sentences;
        }
    }
}
