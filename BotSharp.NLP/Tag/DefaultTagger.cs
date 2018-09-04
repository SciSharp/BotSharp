using System;
using System.Collections.Generic;
using System.Text;
using BotSharp.NLP.Tokenize;

namespace BotSharp.NLP.Tag
{
    /// <summary>
    /// The simplest possible tagger assigns the same tag to each token. 
    /// This may seem to be a rather banal step, but it establishes an important baseline for tagger performance. 
    /// In order to get the best result, we tag each word with the most likely tag. 
    /// </summary>
    public class DefaultTagger : ITagger
    {
        public void Tag(Sentence sentence, TagOptions options)
        {
            
        }

        public void Train(List<Sentence> sentences, TagOptions options)
        {
            
        }
    }
}
