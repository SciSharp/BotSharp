using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Engines.CRFsuite
{
    public class CRFsuiteEntityRecognizer : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }

        public bool Process(Agent agent, JObject data)
        {
            var dc = new DefaultDataContextLoader().GetDefaultDc();
            //var corpus = agent.GrabCorpus(dc);

            // Mock Data
            List<TrainingData> train_sent = new List<TrainingData>();
            train_sent.Add(new TrainingData("Melbourne", "NP", "B-LOC"));
            train_sent.Add(new TrainingData("(", "Fpa", "O"));
            train_sent.Add(new TrainingData("Australia", "NP", "B-LOC"));
            train_sent.Add(new TrainingData(")", "Fpt", "O"));
            train_sent.Add(new TrainingData(",", "Fc", "O"));
            train_sent.Add(new TrainingData("25", "Z", "O"));
            train_sent.Add(new TrainingData("may", "NC", "O"));
            train_sent.Add(new TrainingData("(", "Fpa", "O"));
            train_sent.Add(new TrainingData("EFE", "NC", "B-ORG"));
            train_sent.Add(new TrainingData(")", "Fpt", "O"));
            train_sent.Add(new TrainingData(".", "Fp", "O"));

            List<List<TrainingData>> train_sents = new List<List<TrainingData>>();
            train_sents.Add(train_sent);

            List<ItemSequence> X_train = new List<ItemSequence>();
            train_sents.ForEach(cur_sent => X_train.Add(new ItemSequence(sent2features(cur_sent))));

            StringList sl = new StringList();


            List<StringList> y_train = new List<StringList>();
            train_sents.ForEach(cur_sent => y_train.Add(new StringList(sent2labels(cur_sent))));

            Fit(X_train,y_train);





            return true;
        }
        /*
        public List<TrainingData> Merge(List<Token> sentence, List<Entity> entities )
        {
            List<TrainingData> trainingTuple = new List<TrainingData>();

            HashSet<String> entityWordBag = new HashSet<String>();

            entities.ForEach(entity =>
            {
                String[] words = entity.Value.Split();
                foreach (string word in words)
                {
                    entityWordBag.Add(word);
                }
            });

            sentence.ForEach(token => {
                if (!entityWordBag.Contains(token.Text))
                {
                    trainingTuple.Add(new TrainingData(token.Text, "O", token.Offset));
                }
            });

            entities.ForEach(entity => trainingTuple.Add(new TrainingData(entity.Value, entity.EntityName, entity.Start)));

            trainingTuple.Sort((left, right) => {
                if (left.Start > right.Start)
                    return 1;
                else if (left.Start < right.Start)
                    return -1;
                else
                    return 0;
            });




            return trainingTuple;
        }
        */



        /* Train a model.

         * Parameters
         * ----------
         * X : list of lists of dicts
            Feature dicts for several documents (in a python-crfsuite format).

         * y : list of lists of strings
            Labels for several documents.

         * X_dev : (optional) list of lists of dicts
            Feature dicts used for testing.

         * y_dev : (optional) list of lists of strings
            Labels corresponding to X_dev.
         */
        public void Fit(List<ItemSequence> X, List<StringList> y, List<ItemSequence> X_dev = null, List<StringList> y_dev = null) {
            Trainer trainer = new Trainer();
            for (int i = 0; i < Math.Min(X.Count, y.Count); i++)
            {
                // group ?
                trainer.append(X[i], y[i], 0);
            }

            trainer.train("model_test", X_dev == null ? -1 : 1);
        }

        public Item Word2Features(List<TrainingData> sent, int i) {
            string word = sent[i].Token;
            string postag = sent[i].Tag;

            float bias = 1.0F;
            String wordLower = word.ToLower();
            String wordLast3Char = wordLower.Length >= 3 ? wordLower.Substring(wordLower.Length - 3) : wordLower;
            string patternAllCaptain = @"^[A-Z]+$";
            Boolean isSupper = new Regex(patternAllCaptain).IsMatch(word);
            string patternFirstCaptain = @"^[A-Z]{1}[a-z]+$";
            Boolean isTitle = new Regex(patternFirstCaptain).IsMatch(word);
            string patternAllDigit = @"^[0-9]+$";
            Boolean isDigit = new Regex(patternAllDigit).IsMatch(word);
            String posTag = postag;
            String postagFirst2Char = postag.Length >= 2 ? postag.Substring(0,2) : postag.Substring(0);

            Feature feature = new Feature(bias, wordLower, wordLast3Char, isSupper, isTitle, isDigit, posTag, postagFirst2Char);

            Item curItem = new Item();



            if (i > 0)
            {
                string minusWord = sent[i - 1].Token;
                string minusPostag = sent[i - 1].Tag;

                feature.MinusWordLower = minusWord;
                feature.MinusIsTitle = new Regex(patternFirstCaptain).IsMatch(minusWord);
                feature.MinusIsSupper = new Regex(patternAllCaptain).IsMatch(minusWord);
                feature.MinusPostag = minusPostag;
                feature.MinusPostagFirst2Char = minusPostag.Length >= 2 ? minusPostag.Substring(0, 2) : minusPostag.Substring(0);

                feature.Items.Add(new Attribute($"minusWordLower:{minusWord}", 1.0));
                feature.Items.Add(new Attribute($"minusIsTitle", feature.MinusIsTitle ? 1.0 : 0.0));
                feature.Items.Add(new Attribute($"minusIsSupper", feature.MinusIsSupper ? 1.0 : 0.0));
                feature.Items.Add(new Attribute($"minusPostag:{minusPostag}", 1.0));
                feature.Items.Add(new Attribute($"minusPostagFirst2Char:{feature.MinusPostagFirst2Char}", 1.0));
            }
            else {
                feature.BOS = true;
                feature.Items.Add(new Attribute($"BOS", feature.BOS? 1.0 : 0.0));
            }

            if ( i < sent.Count - 1)
            {
                string plusWord = sent[i + 1].Token;
                string plusPostag = sent[i + 1].Tag;

                feature.PlusWordLower = plusWord;
                feature.PlusIsTitle = new Regex(patternFirstCaptain).IsMatch(plusWord);
                feature.PlusIsSupper = new Regex(patternAllCaptain).IsMatch(plusWord);
                feature.PlusPostag = plusPostag;
                feature.PlusPostagFirst2Char = plusPostag.Length >= 2 ? plusPostag.Substring(0, 2) : plusPostag.Substring(0);

                feature.Items.Add(new Attribute($"minusWordLower:{plusWord}", 1.0));
                feature.Items.Add(new Attribute($"minusIsTitle", feature.PlusIsTitle ? 1.0 : 0.0));
                feature.Items.Add(new Attribute($"minusIsSupper", feature.PlusIsSupper ? 1.0 : 0.0));
                feature.Items.Add(new Attribute($"minusPostag:{plusPostag}", 1.0));
                feature.Items.Add(new Attribute($"minusPostagFirst2Char:{feature.PlusPostagFirst2Char}", 1.0));
            }
            return feature.ToItems();
        }

        public List<Item> sent2features(List<TrainingData> sent)
        {
            List<Item> list = new List<Item>();
            for (int i = 0 ; i < sent.Count; i++ )
            {
                list.Add(Word2Features(sent, i));
            }
            return list;
        }

        public List<String> sent2labels(List<TrainingData> sent)
        {
            List<String> list = new List<String>();
            sent.ForEach(tuple => list.Add(tuple.Entity));
            return list;
        }

        public List<String> sent2tokens(List<TrainingData> sent)
        {
            List<String> list = new List<String>();
            sent.ForEach(tuple => list.Add(tuple.Token));
            return list;
        }
    }
    public class Feature 
    {
        public float Bias { get; set; }
        public String WordLower { get; set; }
        public String WordLast3Char { get; set; }
        public Boolean IsSupper { get; set; }
        public Boolean IsTitle { get; set; }
        public Boolean IsDigit { get; set; }
        public String Postag { get; set; }
        public String PostagFirst2Char { get; set; }

        public Boolean BOS { get; set; }
        public Boolean EOS { get; set; }

        public String PlusWordLower { get; set; }
        public String PlusLast3Char { get; set; }
        public Boolean PlusIsSupper { get; set; }
        public Boolean PlusIsTitle { get; set; }
        public Boolean PlusIsDigit { get; set; }
        public String PlusPostag { get; set; }
        public String PlusPostagFirst2Char { get; set; }

        public String MinusWordLower { get; set; }
        public String MinusLast3Char { get; set; }
        public Boolean MinusIsSupper { get; set; }
        public Boolean MinusIsTitle { get; set; }
        public Boolean MinusIsDigit { get; set; }
        public String MinusPostag { get; set; }
        public String MinusPostagFirst2Char { get; set; }

        public Item Items { get; set; }

        public Feature(float bias, String wordLower, String wordLast3Char, Boolean isSupper, Boolean isTitle, Boolean isDigit, String posTag, String postagFirst2Char)
        {
            this.Bias = bias;
            this.WordLower = wordLower;
            this.WordLast3Char = wordLast3Char;
            this.IsSupper = isSupper;
            this.IsTitle = isTitle;
            this.IsDigit = isDigit;
            this.Postag = posTag;
            this.PostagFirst2Char = postagFirst2Char;
            this.Items = new Item();

            Items.Add(new Attribute($"bias", bias));
            Items.Add(new Attribute($"wordLower:{wordLower}", 1.0));
            Items.Add(new Attribute($"wordLast3Char:{wordLast3Char}", 1.0));
            Items.Add(new Attribute($"isSupper", isSupper? 1.0 : 0.0));
            Items.Add(new Attribute($"isTitle", isTitle ? 1.0 : 0.0));
            Items.Add(new Attribute($"isDigit", isDigit ? 1.0 : 0.0));
            Items.Add(new Attribute($"posTag={posTag}", 1.0));
            Items.Add(new Attribute($"postagFirst2Char:{postagFirst2Char}", 1.0));
        }

        public Item ToItems()
        {
            return this.Items;
        }
    }

    public class TrainingData
    {
        public String Token { get; set; }
        public String Entity { get; set; }
        public String Tag { get; set; }

        public TrainingData(string token, string entity, string tag)
        {
            this.Token = token;
            this.Entity = entity;
            this.Tag = tag;
        }
    }


    public class Token
    {
        public String Text { get; set; }
        public int Offset { get; set; }
        public int End { get; set; }
    }

    public class Entity
    {
        public String EntityName { get; set; }
        public String Value { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
    }


}
