using System;
using System.Collections.Generic;


namespace BotSharp.Algorithm.Bayesian
{
    /// <summary>
    /// Interface IDataSetAccessor
    /// </summary>
    public interface IDataSetAccessor : IEnumerable<TokenCount> 
    {
        /// <summary>
        /// Gets the number of distinct tokens, 
        /// i.e. every token counted at exactly once.
        /// </summary>
        /// <value>The token count.</value>
        /// <seealso cref="SetSize"/>
        long TokenCount { get; }

        /// <summary>
        /// Gets the size of the set.
        /// </summary>
        /// <value>The size of the set.</value>
        /// <seealso cref="TokenCount"/>
        long SetSize { get; }

        /// <summary>
        /// Gets the class.
        /// </summary>
        /// <value>The class.</value>
        IClass Class { get; }

        /// <summary>
        /// Gets the <see cref="TokenInformation{IToken}" /> with the specified token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="alpha">Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied.</param>
        /// <returns>TokenInformation&lt;IToken&gt;.</returns>
        /// <exception cref="System.ArgumentNullException">token</exception>
        TokenInformation<IToken> this[IToken token, double alpha] { get; }

        /// <summary>
        /// Gets the number of occurrences of the given token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>System.Int64.</returns>
        /// <exception cref="System.ArgumentNullException">token</exception>
        /// <seealso cref="GetPercentage"/>
        long GetCount(IToken token);

        /// <summary>
        /// Gets the approximated percentage of the given 
        /// <see cref="IToken" /> in this data set
        /// by determining its occurrence count over the whole population.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="alpha">Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="System.ArgumentNullException">token</exception>
        /// <seealso cref="GetCount" />
        double GetPercentage(IToken token, double alpha);
    }
}