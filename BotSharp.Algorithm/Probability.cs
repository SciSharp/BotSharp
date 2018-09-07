using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Algorithm
{
    /// <summary>
    /// In probability theory and statistics, a probability distribution is a mathematical function 
    /// that provides the probabilities of occurrence of different possible outcomes in an experiment.
    /// https://en.wikipedia.org/wiki/Probability_distribution
    /// </summary>
    public class Probability
    {
        /// <summary>
        /// one value of all samples
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// the number of times that something happens within a particular period of time
        /// </summary>
        public int Freq { get; set; }

        /// <summary>
        /// how likely something is, sometimes calculated in a mathematical way
        /// </summary>
        public double Prob { get; set; }

        public override string ToString()
        {
            return $"{Value} {Freq} {Prob}";
        }
    }
}
