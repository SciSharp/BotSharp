using System.Collections.Generic;

namespace BotSharp.Algorithm.Bayesian
{
    /// <summary>
    /// Interface ITrainingSetAccerssor
    /// </summary>
    public interface ITrainingSetAccessor : IEnumerable<IDataSet> 
    {
        /// <summary>
        /// Gets the <see cref="IDataSet"/> with the specified class.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <returns>IDataSet&lt;TClass, TToken&gt;.</returns>
        /// <exception cref="System.ArgumentException">No data set was registered for the given class;class</exception>
        IDataSet this[IClass @class] { get; }
    }
}