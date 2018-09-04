using System;
using System.Diagnostics;

namespace BotSharp.Algorithm.Bayesian
{
    /// <summary>
    /// Class ClassBase.
    /// </summary>
    [DebuggerDisplay("Class {Name}, base P = {Probability}")]
    public abstract class ClassBase : IClass
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the class' base probability.
        /// </summary>
        /// <value>The probability.</value>
        public double Probability { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassBase" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="probability">The probability.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// probability;Class base probability must be greater than or equal to zero.
        /// or
        /// probability;Class base probability must be less than or equal to one.
        /// </exception>
        protected ClassBase(string name, double probability)
        {
            if (ReferenceEquals(name, null)) throw new ArgumentNullException("name");
            if (probability < 0) throw new ArgumentOutOfRangeException("probability", "Class base probability must be greater than or equal to zero.");
            if (probability > 1) throw new ArgumentOutOfRangeException("probability", "Class base probability must be less than or equal to one.");

            Name = name;
            Probability = probability;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public abstract bool Equals(IClass other);
    }
}