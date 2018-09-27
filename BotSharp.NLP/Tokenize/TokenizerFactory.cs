using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.NLP.Tokenize
{
    /// <summary>
    /// BotSharp Tokenizer Factory
    /// Tokenizers divide strings into lists of substrings.
    /// The particular tokenizer requires implement interface 
    /// models to be installed.BotSharp.NLP also provides a simpler, regular-expression based tokenizer, which splits text on whitespace and punctuation.
    /// </summary>
    public class TokenizerFactory
    {
        private SupportedLanguage _lang;

        private ITokenizer _tokenizer;

        private TokenizationOptions _options;

        public ITokenizer GetTokenizer<ITokenize>() where ITokenize : ITokenizer, new()
        {
            return _tokenizer = new ITokenize();
        }

        public ITokenizer GetTokenizer(string name)
        {
            List<Type> types = new List<Type>();

            types.AddRange(Assembly.Load(new AssemblyName("BotSharp.Core"))
                .GetTypes().Where(x => !x.IsAbstract && !x.FullName.StartsWith("<>f__AnonymousType")).ToList());

            types.AddRange(Assembly.Load(new AssemblyName("BotSharp.NLP"))
                .GetTypes().Where(x => !x.IsAbstract && !x.FullName.StartsWith("<>f__AnonymousType")).ToList());

            Type type = types.FirstOrDefault(x => x.Name == name);
            var instance = (ITokenizer)Activator.CreateInstance(type);

            return _tokenizer = instance;
        }

        public TokenizerFactory(TokenizationOptions options, SupportedLanguage lang)
        {
            _lang = lang;
            _options = options;
        }

        public List<Token> Tokenize(string sentence)
        {
            var tokens = _tokenizer.Tokenize(sentence, _options);
            tokens.ForEach(x => x.Lemma = x.Text.ToLower());
            return tokens;
        }

        public List<Sentence> Tokenize(List<String> sentences)
        {
            var sents = sentences.Select(s => new Sentence { Text = s }).ToList();

            Parallel.ForEach(sents, (sentence) =>
            {
                sentence.Words = Tokenize(sentence.Text);
                sentence.Words.ForEach(x => x.Lemma = x.Text.ToLower());
            });

            return sents;
        }

        private class ParallelToken
        {
            public String Text { get; set; }

            public List<Token> Tokens { get; set; }
        }
    }
}
