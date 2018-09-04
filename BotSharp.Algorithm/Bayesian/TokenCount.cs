using System;


namespace BotSharp.Algorithm.Bayesian
{
    /// <summary>
    /// Struct TokenCount
    /// </summary>
    public struct TokenCount
    {
        /// <summary>
        /// The token
        /// </summary>
        public readonly IToken Token;
        
        /// <summary>
        /// The number of occurrences
        /// </summary>
        public readonly long Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenCount" /> struct.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="System.ArgumentNullException">token</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">count;Count must be positive or zero.</exception>
        public TokenCount(IToken token, long count)
        {
            if (ReferenceEquals(token, null)) throw new ArgumentNullException("token");
            if (count < 0) throw new ArgumentOutOfRangeException("count", count, "Count must be positive or zero.");

            Token = token;
            Count = count;
        }
    }
}
