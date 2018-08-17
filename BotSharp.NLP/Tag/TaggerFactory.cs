using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Tag
{
    public class TaggerFactory<ITag> where ITag : ITagger, new()
    {
        private SupportedLanguage _lang;

        private ITag _tagger;

        private TagOptions _options;

        public TaggerFactory(TagOptions options, SupportedLanguage lang)
        {
            _lang = lang;
            _options = options;
            _tagger = new ITag();
        }

        public void Tag(Sentence sentence)
        {
            _tagger.Tag(sentence, _options);
        }
    }
}
