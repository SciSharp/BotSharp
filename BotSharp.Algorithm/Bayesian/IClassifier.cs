using System;
using System.Collections.Generic;


namespace BotSharp.Algorithm.Bayesian
{
    /// <summary>
    /// Interface IClassifier
    /// </summary>
    public interface IClassifier
    {
        /// <summary>
        /// Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied.
        /// <para>
        /// Laplace smoothing is required in the context or rare (i.e. untrained) tokens or tokens
        /// that do not appear in some classes. With smoothing disabled, these tokens result
        /// in a zero probability for the whole class. To counter that, a positive ("alpha")
        /// value for smoothing can be set.
        /// </para>
        /// </summary>
        double SmoothingAlpha { get; set; }
        
        /// <summary>
        /// Calculates the probability of having the <see cref="IClass"/> 
        /// given the occurrence of the <see cref="IToken"/>.
        /// </summary>
        /// <param name="classUnderTest">The class under test.</param>
        /// <param name="token">The token.</param>
        /// <param name="alpha">Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied, setting to <see langword="null"/> defaults to the values set in <see cref="SmoothingAlpha"/>.</param>
        /// <returns>System.Double.</returns>
        double CalculateProbability(IClass classUnderTest, IToken token, double? alpha = null);

        /// <summary>
        /// Calculates the probability of having the 
        /// <see cref="IClass" />
        /// given the occurrence of the 
        /// <see cref="IToken" />.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="alpha">Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied, setting to <see langword="null"/> defaults to the values set in <see cref="SmoothingAlpha"/>.</param>
        /// <returns>System.Double.</returns>
        IEnumerable<ConditionalProbability> CalculateProbabilities(IToken token, double? alpha = null);

        /// <summary>
        /// Calculates the probability of having the
        /// <see cref="IClass" />
        /// given the occurrence of the
        /// <see cref="IToken" />.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <param name="alpha">Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied, setting to <see langword="null"/> defaults to the values set in <see cref="SmoothingAlpha"/>.</param>
        /// <returns>System.Double.</returns>
        IEnumerable<CombinedConditionalProbability> CalculateProbabilities(ICollection<IToken> tokens, double? alpha = null);
    }
}