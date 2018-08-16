using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Tag
{
    /// <summary>
    /// Part-Of-Speech tagging (or POS tagging, for short) is one of the main components of almost any NLP analysis. 
    /// The task of POS-tagging simply implies labelling words with their appropriate Part-Of-Speech (Noun, Verb, Adjective, Adverb, Pronoun, …).
    /// </summary>
    public interface ITagger
    {
        /// <summary>
        /// Language
        /// </summary>
        SupportedLanguage Lang { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sentences">A tagged corpus. Each item should be a list of tokens.</param>
        /// <param name="options"></param>
        /// <returns></returns>
        void Train(List<Sentence> sentences, TagOptions options);

        void Tag(Sentence sentence, TagOptions options);
    }
}
