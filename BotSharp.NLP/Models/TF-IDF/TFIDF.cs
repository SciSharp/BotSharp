using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace BotSharp.NLP.Models.TF_IDF
{
    /// <summary>
    /// Copyright (c) 2018 Bo Peng
    /// 
    /// Permission is hereby granted, free of charge, to any person obtaining
    /// a copy of this software and associated documentation files (the
    /// "Software"), to deal in the Software without restriction, including
    /// without limitation the rights to use, copy, modify, merge, publish,
    /// distribute, sublicense, and/or sell copies of the Software, and to
    /// permit persons to whom the Software is furnished to do so, subject to
    /// the following conditions:
    /// 
    /// The above copyright notice and this permission notice shall be
    /// included in all copies or substantial portions of the Software.
    /// </summary>
    public class TFIDF
    {
        List<string> vocabulary { get; set; }

        public TFIDF()
        {
        }

        /// <summary>
        /// Document vocabulary, containing each word's IDF value.
        /// </summary>
        private static Dictionary<string, double> _vocabularyIDF = new Dictionary<string, double>();

        public static List<List<double>> GetTFIDFWeightsVectors(string[] documents, int vocabularyThreshold = 1)
        {
            List<List<string>> stemmedDocs;
            List<string> vocabulary;
            // Get the vocabulary and stem the documents at the same time.
            vocabulary = GetVocabulary(documents, out stemmedDocs, vocabularyThreshold);
            if (_vocabularyIDF.Count == 0)
            {
                // Calculate the IDF for each vocabulary term.
                foreach (var term in vocabulary)
                {
                    double numberOfDocsContainingTerm = stemmedDocs.Where(d => d.Contains(term)).Count();
                    _vocabularyIDF[term] = Math.Log((double)stemmedDocs.Count / ((double)1 + numberOfDocsContainingTerm));
                }
            }
            // Transform each document into a vector of tfidf values.
            List<List<double>> vectors = new List<List<double>>();
            foreach (var doc in stemmedDocs)
            {
                List<double> vector = new List<double>();
                foreach (string word in doc)
                {
                    double tf = doc.Where(d => d == word).Count();
                    double tfidf = tf * _vocabularyIDF[word];
                    vector.Add(tfidf);
                }
                vectors.Add(vector);
            }
            return vectors;
        }
        

        /// <summary>
        /// Normalizes a TF*IDF array of vectors using L2-Norm.
        /// Xi = Xi / Sqrt(X0^2 + X1^2 + .. + Xn^2)
        /// </summary>
        /// <param name="vectors">List<List<double>></param>
        /// <returns>List<List<double>></returns>
        public static List<List<double>> Normalize(List<List<double>> vectors)
        {
            // Normalize the vectors using L2-Norm.
            List<List<double>> normalizedVectors = new List<List<double>>();
            foreach (var vector in vectors)
            {
                var normalized = Normalize(vector);
                normalizedVectors.Add(normalized);
            }

            return normalizedVectors;
        }

        /// <summary>
        /// Normalizes a TF*IDF vector using L2-Norm.
        /// Xi = Xi / Sqrt(X0^2 + X1^2 + .. + Xn^2)
        /// </summary>
        /// <param name="vectors"> List<double> </param>
        /// <returns> List<double> </returns>
        public static List<double> Normalize(List<double> vector)
        {
            List<double> result = new List<double>();

            double sumSquared = 0;
            foreach (var value in vector)
            {
                sumSquared += value * value;
            }

            double SqrtSumSquared = Math.Sqrt(sumSquared);

            foreach (var value in vector)
            {
                // L2-norm: Xi = Xi / Sqrt(X0^2 + X1^2 + .. + Xn^2)
                result.Add(value / SqrtSumSquared);
            }
            return result;
        }

        /// <summary>
        /// Saves the TFIDF vocabulary to disk.
        /// </summary>
        /// <param name="filePath">File path</param>
        public static void Save(string filePath = "vocabulary.dat")
        {
            // Save result to disk.
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, _vocabularyIDF);
            }
        }

        /// <summary>
        /// Loads the TFIDF vocabulary from disk.
        /// </summary>
        /// <param name="filePath">File path</param>
        public static void Load(string filePath = "vocabulary.dat")
        {
            // Load from disk.
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                _vocabularyIDF = (Dictionary<string, double>)formatter.Deserialize(fs);
            }
        }
        
        /// <summary>
        /// Parses and tokenizes a list of documents, returning a vocabulary of words.
        /// </summary>
        /// <param name="docs">string[]</param>
        /// <param name="stemmedDocs">List of List of string</param>
        /// <returns>Vocabulary (list of strings)</returns>
        private static List<string> GetVocabulary(string[] docs, out List<List<string>> stemmedDocs, int vocabularyThreshold)
        {
            List<string> vocabulary = new List<string>();
            Dictionary<string, int> wordCountList = new Dictionary<string, int>();
            stemmedDocs = new List<List<string>>();
            int docIndex = 0;
            var tokenizer = new TokenizerFactory<RegexTokenizer>(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WHITE_SPACE
            }, SupportedLanguage.English);

            foreach (var doc in docs)
            {
                List<string> stemmedDoc = new List<string>();
                docIndex++;
                if (docIndex % 100 == 0)
                {
                    Console.WriteLine("Processing " + docIndex + "/" + docs.Length);
                }

                List<Token> tokens = tokenizer.Tokenize(doc);
                List<string> list = new List<string>(); 
                tokenizer.Tokenize(doc).ForEach( token => {
                    list.Add(token.Text.ToLower());
                });
                string[] parts2 = list.ToArray();
                //string[] parts2 = Tokenize(doc);
                List<string> words = new List<string>();
                foreach (string part in parts2)
                {
                    // Strip non-alphanumeric characters.
                    string stripped = Regex.Replace(part, "[^a-zA-Z0-9]", "");
                    try
                    {
                        var english = new EnglishWord(stripped);
                        string stem = english.Stem;
                        words.Add(stem);

                        if (stem.Length > 0)
                        {
                            // Build the word count list.
                            if (wordCountList.ContainsKey(stem))
                            {
                                wordCountList[stem]++;
                            }
                            else
                            {
                                wordCountList.Add(stem, 0);
                            }
                            stemmedDoc.Add(stem);
                        }
                    }
                    catch
                    {
                    }
                }
                stemmedDocs.Add(stemmedDoc);
            }
            // Get the top words.
            var vocabList = wordCountList.Where(w => w.Value >= vocabularyThreshold);
            foreach (var item in vocabList)
            {
                vocabulary.Add(item.Key);
            }
            return vocabulary;
        }
        
        
    }
    public class EnglishWord
    {
        public EnglishWord(string input)
        {
            this.Original = input;
            this.Stem = input;
            this.Length = input.Length;
        }

        public string Stem { get; set; }
        public string Original { get; }
        public int Length { get; }
    }
}
