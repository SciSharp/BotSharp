using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BotSharp.Algorithm.Bayesian
{
    /// <summary>
    /// Struct CombinedConditionalProbability
    /// </summary>
    [DebuggerDisplay("P({Class}|{TokenProbabilities.Count} tokens)={Probability}")]
    public struct CombinedConditionalProbability
    {
        /// <summary>
        /// The class
        /// </summary>
        public readonly IClass Class;
        
        /// <summary>
        /// The token probabilities
        /// </summary>
        public ICollection<ConditionalProbability> TokenProbabilities;

        /// <summary>
        /// The probability
        /// </summary>
        public double Probability;

        /// <summary>
        /// Initializes a new instance of the <see cref="CombinedConditionalProbability" /> struct.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <param name="probability">The probability.</param>
        /// <param name="tokenProbabilities">The tokenProbabilities.</param>
        public CombinedConditionalProbability(IClass @class, double probability, ICollection<ConditionalProbability> tokenProbabilities)
        {
            if (ReferenceEquals(@class, null)) throw new ArgumentNullException("class");
            if (ReferenceEquals(tokenProbabilities, null)) throw new ArgumentNullException("tokenProbabilities");
            if (probability < 0) throw new ArgumentOutOfRangeException("probability", probability, "Probability must greater than or equal to zero");
            if (probability > 1) throw new ArgumentOutOfRangeException("probability", probability, "Probability must less than or equal to one");

            Class = @class;
            TokenProbabilities = tokenProbabilities;
            Probability = probability;
        }
    }
}
