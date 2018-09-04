using System.Collections.Generic;


namespace BotSharp.Algorithm.Bayesian
{
    /// <summary>
    /// Interface ITokenRegistration
    /// </summary>
    public interface ITokenRegistration
    {
        /// <summary>
        /// Adds the given tokens a single time, incrementing the <see cref="DataSet.SetSize"/>
        /// and, at the first addition, the <see cref="DataSet.TokenCount"/>.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="additionalTokens">The additional tokens.</param>
        /// <exception cref="System.ArgumentNullException">
        /// token
        /// or
        /// additionalTokens
        /// </exception>
        void AddToken(IToken token, params IToken[] additionalTokens);

        /// <summary>
        /// Adds the given tokens a single time, incrementing the <see cref="DataSet.SetSize"/>
        /// and, at the first addition, the <see cref="DataSet.TokenCount"/>.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <exception cref="System.ArgumentNullException">tokens</exception>
        void AddToken(IEnumerable<IToken> tokens);

        /// <summary>
        /// Removes the given tokens a single time, decrementing the <see cref="DataSet.SetSize"/> and,
        /// eventually, the <see cref="DataSet.TokenCount"/>.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="additionalTokens">The additional tokens.</param>
        /// <exception cref="System.ArgumentNullException">
        /// token
        /// or
        /// additionalTokens
        /// </exception>
        /// <seealso cref="PurgeToken(IToken,IToken[])"/>
        void RemoveTokenOnce(IToken token, params IToken[] additionalTokens);

        /// <summary>
        /// Removes the given tokens a single time, decrementing the <see cref="DataSet.SetSize"/> and,
        /// eventually, the <see cref="DataSet.TokenCount"/>.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <exception cref="System.ArgumentNullException">tokens</exception>
        /// <seealso cref="PurgeToken(System.Collections.Generic.IEnumerable{IToken})"/>
        void RemoveTokenOnce(IEnumerable<IToken> tokens);

        /// <summary>
        /// Removes the given tokens a single time, decrementing the <see cref="IDataSet.SetSize"/> and,
        /// eventually, the <see cref="IDataSet.TokenCount"/>.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="additionalTokens">The additional tokens.</param>
        /// <exception cref="System.ArgumentNullException">
        /// token
        /// or
        /// additionalTokens
        /// </exception>
        /// <seealso cref="RemoveTokenOnce(IToken,IToken[])"/>
        void PurgeToken(IToken token, params IToken[] additionalTokens);

        /// <summary>
        /// Removes the given tokens a single time, decrementing the <see cref="IDataSet.SetSize"/> and,
        /// eventually, the <see cref="IDataSet.TokenCount"/>.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <exception cref="System.ArgumentNullException">tokens</exception>
        /// <seealso cref="RemoveTokenOnce(System.Collections.Generic.IEnumerable{IToken})"/>
        void PurgeToken(IEnumerable<IToken> tokens);
    }
}