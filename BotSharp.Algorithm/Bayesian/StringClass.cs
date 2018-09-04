using System;


namespace BotSharp.Algorithm.Bayesian
{
    /// <summary>
    /// Class StringClass. This class cannot be inherited.
    /// </summary>
    public sealed class StringClass : ClassBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringClass" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="probability">The probability.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// probability;Class base probability must be greater than or equal to zero.
        /// or
        /// probability;Class base probability must be less than or equal to one.
        /// </exception>
        public StringClass(string name, double probability)
            : base(name, probability)
        {
        }

        /// <summary>
        /// Determines whether the specified <see cref="StringClass" /> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="T:System.Object" /> to compare with the current <see cref="T:StringClass" />.</param>
        /// <returns><see langword="true" /> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <see langword="false" />.</returns>
        public override bool Equals(IClass other)
        {
            var otherAsObject = (object) other;
            return Equals(otherAsObject);
        }

        /// <summary>
        /// Determines whether the specified <see cref="StringClass" /> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="T:System.Object" /> to compare with the current <see cref="T:StringClass" />.</param>
        /// <returns><see langword="true" /> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <see langword="false" />.</returns>
        private bool Equals(StringClass other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(other, this)) return true;
            return String.Equals(Name, other.Name) 
                && Math.Abs(Probability - other.Probability) < 0.0001D;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object" /> to compare with the current <see cref="T:System.Object" />.</param>
        /// <returns><see langword="true" /> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <see langword="false" />.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is StringClass && Equals((StringClass) obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            var hash = 27;
            hash = (13 * hash) + Name.GetHashCode();
            hash = (13 * hash) + Probability.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
