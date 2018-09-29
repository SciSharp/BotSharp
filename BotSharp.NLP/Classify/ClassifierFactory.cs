using Bigtree.Algorithm.Features;
using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BotSharp.NLP.Classify
{
    public class ClassifierFactory<IFeatureExtractor> 
        where IFeatureExtractor : ITextFeatureExtractor, new()
    {
        private SupportedLanguage _lang;

        private IClassifier _classifier;

        private ClassifyOptions _options;

        private IFeatureExtractor featureExtractor;

        public ClassifierFactory(ClassifyOptions options, SupportedLanguage lang)
        {
            _lang = lang;
            _options = options;
            featureExtractor = new IFeatureExtractor();
        }

        public IClassifier GetClassifer(string name)
        {
            List<Type> types = new List<Type>();

            types.AddRange(Assembly.Load(new AssemblyName("BotSharp.Core"))
                .GetTypes().Where(x => !x.IsAbstract && !x.FullName.StartsWith("<>f__AnonymousType")).ToList());

            types.AddRange(Assembly.Load(new AssemblyName("BotSharp.NLP"))
                .GetTypes().Where(x => !x.IsAbstract && !x.FullName.StartsWith("<>f__AnonymousType")).ToList());

            Type type = types.FirstOrDefault(x => x.Name == name);
            var instance = (IClassifier)Activator.CreateInstance(type);

            return _classifier = instance;
        }

        public void Train(List<Sentence> sentences)
        {
            _classifier.Train(sentences, _options);
            _classifier.SaveModel(_options);
        }

        public List<Tuple<string, double>> Classify(Sentence sentence)
        {
            var options = new ClassifyOptions
            {
                ModelFilePath = _options.ModelFilePath,
                ModelDir = _options.ModelDir,
                ModelName = _options.ModelName
            };

            _classifier.LoadModel(options);

            var classes = _classifier.Classify(sentence, options);

            classes = classes.OrderByDescending(x => x.Item2).ToList();

            return classes;
        }
    }
}
