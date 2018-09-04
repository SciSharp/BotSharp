using System;
using System.ComponentModel;


namespace BotSharp.Algorithm.Bayesian
{
    /// <summary>
    /// Interface IClass
    /// </summary>
    public interface IClass : IEquatable<IClass>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets or sets the class' base probability.
        /// </summary>
        /// <value>The probability.</value>
        [DefaultValue(1)]
        double Probability { get; set; }
    }
}
