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
            if (String.IsNullOrEmpty(options.LabelPrefix))
            {
                options.LabelPrefix = "__label__";
            }

            var sentences = new List<Sentence>();
            using (StreamReader reader = new StreamReader(Path.Combine(options.DataDir, options.FileName)))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        var ms = Regex.Matches(line, options.LabelPrefix + @"\S+")
                            .Cast<Match>()
                            .ToList();

                        var text = line.Substring(ms.Last().Index + ms.Last().Length + 1);

                        ms.ForEach(m =>
                        {
                            sentences.Add(new Sentence
                            {
                                Label = m.Value.Substring(options.LabelPrefix.Length),
                                Text = text
                            });
                        });

                    }
                }
            }

            return sentences;
        }
    }
}
