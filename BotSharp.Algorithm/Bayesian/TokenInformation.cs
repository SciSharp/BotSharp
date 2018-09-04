using System;


namespace BotSharp.Algorithm.Bayesian
{
    /// <summary>
    /// Struct TokenInformation
    /// </summary>
    public struct TokenInformation<TToken>
        where TToken: IToken
    {
        /// <summary>
        /// The token
        /// </summary>
        public readonly TToken Token;
        
        /// <summary>
        /// The count in the class
        /// </summary>
        public long Count;

        /// <summary>
        /// The occurrence percentage of the token in the class.
        /// </summary>
        public double Percentage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenInformation{TToken}" /> struct.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="count">The count.</param>
        /// <param name="percentage">The percentage.</param>
        /// <exception cref="System.ArgumentNullException">token</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// count;Count must be positive or zero
        /// or
        /// percentage;Percentage must be positive or zero
        /// </exception>
        public TokenInformation(TToken token, long count, double percentage)
        {
            if (ReferenceEquals(token, null)) throw new ArgumentNullException("token");
            if (count < 0) throw new ArgumentOutOfRangeException("count", count, "Count must be positive or zero");
            if (percentage < 0) throw new ArgumentOutOfRangeException("percentage", percentage, "Percentage must be positive or zero");

            Token = token;
            Count = count;
            Percentage = percentage;
        }
    }
}
