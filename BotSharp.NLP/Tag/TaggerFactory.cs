using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Tag
{
    public class TaggerFactory<ITag> where ITag : ITagger, new()
    {
        private ITag _tagger;

        public TaggerFactory()
        {
            _tagger = new ITag();
        }

        public void Tag(Sentence sentence, TagOptions options)
        {
            _tagger.Tag(sentence, options);
        }
    }
}
