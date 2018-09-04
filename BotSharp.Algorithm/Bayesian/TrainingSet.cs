using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;


namespace BotSharp.Algorithm.Bayesian
{
    /// <summary>
    /// Class TrainingSet. This class cannot be inherited.
    /// </summary>
    public sealed class TrainingSet : ITrainingSet
    {
        /// <summary>
        /// The data sets
        /// </summary>
        private readonly ConcurrentDictionary<IClass, IDataSet> _dataSets = new ConcurrentDictionary<IClass, IDataSet>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TrainingSet"/> class.
        /// </summary>
        public TrainingSet()
        {           
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrainingSet"/> class.
        /// </summary>
        /// <param name="dataSets">The data sets.</param>
        public TrainingSet(IEnumerable<IDataSet> dataSets)
        {
            Add(dataSets);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrainingSet"/> class.
        /// </summary>
        /// <param name="dataSet">The data set.</param>
        /// <param name="additionalDataSets">The additional data sets.</param>
        public TrainingSet(IDataSet dataSet, params IDataSet[] additionalDataSets)
        {
            Add(dataSet, additionalDataSets);
        }

        /// <summary>
        /// Gets the <see cref="IDataSet"/> with the specified class.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <returns>IDataSet&lt;IClass, IToken&gt;.</returns>
        /// <exception cref="System.ArgumentException">No data set was registered for the given class;class</exception>
        public IDataSet this[IClass @class]
        {
            get
            {
                IDataSet set;
                if (_dataSets.TryGetValue(@class, out set)) return set;
                throw new ArgumentException("No data set was registered for the given class", "class");
            }
        }

        /// <summary>
        /// Adds the specified data set.
        /// </summary>
        /// <param name="dataSet">The data set.</param>
        /// <param name="additionalDataSets">The additional data sets.</param>
        /// <exception cref="System.ArgumentNullException">dataSet</exception>
        /// <exception cref="System.ArgumentException">A data set for a given class was already registered.</exception>
        public void Add(IDataSet dataSet, params IDataSet[] additionalDataSets)
        {
            if (ReferenceEquals(dataSet, null)) throw new ArgumentNullException("dataSet");

            try
            {
                AddInternal(dataSet);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException("A data set for a given class was already registered.", e);
            }

            // may throw, that's anticipated
            Add(additionalDataSets);
        }

        /// <summary>
        /// Adds the specified data sets.
        /// </summary>
        /// <param name="dataSets">The data sets.</param>
        /// <exception cref="System.ArgumentNullException">dataSets</exception>
        /// <exception cref="System.ArgumentException">A data set for a given class was already registered.</exception>
        public void Add(IEnumerable<IDataSet> dataSets)
        {
            if (ReferenceEquals(dataSets, null)) throw new ArgumentNullException("dataSets");

            try
            {
                foreach (var dataSet in dataSets)
                {
                    AddInternal(dataSet);
                }
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException("A data set for a given class was already registered.", e);
            }
        }


        /// <summary>
        /// Adds the data set internally.
        /// </summary>
        /// <param name="dataSet">The data set.</param>
        /// <exception cref="System.ArgumentException">Data set for the given class was already registered.</exception>
        private void AddInternal(IDataSet dataSet)
        {
            if (!_dataSets.TryAdd(dataSet.Class, dataSet))
            {
                throw new ArgumentException("Data set for the given class was already registered.");
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        public IEnumerator<IDataSet> GetEnumerator()
        {
            return _dataSets.Select(dataSet => dataSet.Value).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
