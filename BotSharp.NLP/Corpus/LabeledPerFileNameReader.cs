using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotSharp.NLP.Corpus
{
    /// <summary>
    /// It used to read labeled data which is seperated by file.
    /// The same category data is in one file.
    /// File name is the label.
    /// </summary>
    public class LabeledPerFileNameReader
    {
        public List<Sentence> Read(ReaderOptions options)
        {
            string lable = options.FileName.Split('.')[0];

            var sentences = new List<Sentence>();
            using (StreamReader reader = new StreamReader(Path.Combine(options.DataDir, options.FileName)))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        sentences.Add(new Sentence
                        {
                            Labels = new List<string> { lable },
                            Text = line
                        });
                    }
                }
            }

            return sentences;
        }
    }
}
