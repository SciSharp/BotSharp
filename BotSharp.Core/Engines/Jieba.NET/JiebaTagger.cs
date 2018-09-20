using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.NLP;
using BotSharp.NLP.Tag;
using JiebaNet.Segmenter.PosSeg;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.Jieba.NET
{
    public class JiebaTagger : ITagger
    {
        private PosSegmenter posSeg;

        public void Tag(Sentence sentence, TagOptions options)
        {
            Init();

            var tokens = posSeg.Cut(sentence.Text).ToList();

            for(int i = 0; i < sentence.Words.Count; i++)
            {
                sentence.Words[i].Pos = tokens[i].Flag;
                sentence.Words[i].Tag = tokens[i].Flag;
            }
        }

        public void Train(List<Sentence> sentences, TagOptions options)
        {
            
        }

        private void Init()
        {
            if (posSeg == null)
            {
                posSeg = new PosSegmenter();
            }
        }
    }
}
