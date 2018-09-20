using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BotSharp.NLP.Tag
{
    public class TaggerFactory
    {
        private SupportedLanguage _lang;

        private ITagger _tagger;

        private TagOptions _options;

        public TaggerFactory(TagOptions options, SupportedLanguage lang)
        {
            _lang = lang;
            _options = options;
        }

        public ITagger GetTagger<ITag>() where ITag : ITagger, new()
        {
            return _tagger = new ITag();
        }

        public ITagger GetTagger(string name)
        {
            List<Type> types = new List<Type>();

            types.AddRange(Assembly.Load(new AssemblyName("BotSharp.Core"))
                .GetTypes().Where(x => !x.IsAbstract && !x.FullName.StartsWith("<>f__AnonymousType")).ToList());

            types.AddRange(Assembly.Load(new AssemblyName("BotSharp.NLP"))
                .GetTypes().Where(x => !x.IsAbstract && !x.FullName.StartsWith("<>f__AnonymousType")).ToList());

            Type type = types.FirstOrDefault(x => x.Name == name);
            var instance = (ITagger)Activator.CreateInstance(type);

            return _tagger = instance;
        }

        public void Tag(Sentence sentence)
        {
            _tagger.Tag(sentence, _options);
        }
    }
}
