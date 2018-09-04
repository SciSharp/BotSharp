using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;


namespace BotSharp.Algorithm.Bayesian
{
    /// <summary>
    /// Class NaiveClassifier. This class cannot be inherited.
    /// <para>
    /// Assumes that all token occurrences are statistically independent.
    /// </para>
    /// </summary>
    public sealed class NaiveClassifier : IClassifier
    {
        /// <summary>
        /// The training sets
        /// </summary>
        private readonly ITrainingSetAccessor _trainingSets;

        /// <summary>
        /// Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied.
        /// </summary>
        private double _smoothingAlpha = 0.01D;

        /// <summary>
        /// Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied.
        /// </summary>
        [DefaultValue(0.01D)]
        public double SmoothingAlpha
        {
            get { return _smoothingAlpha; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("value", value, "Value must be greater than zero.");
                _smoothingAlpha = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NaiveClassifier"/> class.
        /// </summary>
        /// <param name="trainingSets">The training sets.</param>
        /// <exception cref="System.ArgumentNullException">trainingSets</exception>
        public NaiveClassifier(ITrainingSetAccessor trainingSets)
        {
            if (ReferenceEquals(trainingSets, null)) throw new ArgumentNullException("trainingSets");
            _trainingSets = trainingSets;
        }

        /// <summary>
        /// Calculates the probability of having the <see cref="IClass"/> 
        /// given the occurrence of the <see cref="IToken"/>.
        /// </summary>
        /// <param name="classUnderTest">The class under test.</param>
        /// <param name="token">The token.</param>
        /// <param name="alpha">Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied.</param>
        /// <returns>System.Double.</returns>
        public double CalculateProbability(IClass classUnderTest, IToken token, double? alpha = null)
        {
            var smoothingAlpha = alpha ?? _smoothingAlpha;

            ICollection<IDataSetAccessor> remainingSets;
            var setForClassUnderTest = SplitDataSets(classUnderTest, out remainingSets);

            // calculate the token's probability in the class under test
            var percentageInClassUnderTest = setForClassUnderTest.GetPercentage(token, smoothingAlpha);
            var probabilityInClassUnderTest = percentageInClassUnderTest * classUnderTest.Probability;

            // calculate the token's probabilities for the remaining classes
            double sumOfRemainingProbabilites;
            CalculateTokenProbabilityGivenClass(token, remainingSets, out sumOfRemainingProbabilites, smoothingAlpha).Run();

            // calculate total probability
            var totalProbability = probabilityInClassUnderTest + sumOfRemainingProbabilites;

            // calculate the class' probability given the token
            var probabilityForClass = probabilityInClassUnderTest/totalProbability;

            // correct for rare words
            return probabilityForClass;
        }
        
        /// <summary>
        /// Calculates the probability of having the 
        /// <see cref="IClass" />
        /// given the occurrence of the 
        /// <see cref="IToken" />.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="alpha">Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied.</param>
        /// <returns>System.Double.</returns>
        public IEnumerable<ConditionalProbability> CalculateProbabilities(IToken token, double? alpha = null)
        {
            var smoothingAlpha = alpha ?? _smoothingAlpha;

            // calculate the token's probabilities for all classes
            double totalProbability;
            var probabilities = CalculateTokenProbabilityGivenClass(token, _trainingSets, out totalProbability, smoothingAlpha);
            
            // apply Bayes theorem
            var inverseOfTotalProbability = 1.0D/totalProbability;
            return from cp in probabilities
                   let conditionalProbability = cp.Probability * inverseOfTotalProbability
                   select new ConditionalProbability(cp.Class, cp.Token, conditionalProbability, cp.Occurrence);
        }

        /// <summary>
        /// Calculates the probability of having the
        /// <see cref="IClass" />
        /// given the occurrence of the
        /// <see cref="IToken" />.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <param name="alpha">Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied.</param>
        /// <returns>System.Double.</returns>
        public IEnumerable<CombinedConditionalProbability> CalculateProbabilities(ICollection<IToken> tokens, double? alpha = null)
        {
            var smoothingAlpha = alpha ?? _smoothingAlpha;

            var cpgs = tokens
                .SelectMany(token => CalculateProbabilities(token, smoothingAlpha))
                .GroupBy(cp => cp.Class)
                .ToCollection();

            return from @group in cpgs
                let cps = @group.ToCollection()
                let eta = cps.Select(cp => cp.Probability)
                    .Sum(p => Math.Log(1 - p) - Math.Log(p))
                let probability = 1/(1 + Math.Exp(eta))
                select new CombinedConditionalProbability(@group.Key, probability, cps);
        }

        /// <summary>
        /// Calculates the token probabilities given a class.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="sets">The sets.</param>
        /// <param name="alpha">Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied.</param>
        /// <returns>IEnumerable&lt;ConditionalProbability&lt;IClass, IToken&gt;&gt;.</returns>
        private IEnumerable<ConditionalProbability> CalculateTokenProbabilityGivenClass(IToken token, IEnumerable<IDataSetAccessor> sets, double alpha)
        {
            return from set in sets
                let @class = set.Class
                let classProbability = @class.Probability
                let percentageInClass = set.GetPercentage(token, alpha)
                let countInClass = set.GetCount(token)
                let probabilityInClass = percentageInClass*classProbability
                select new ConditionalProbability(@class, token, probabilityInClass, countInClass);
        }

        /// <summary>
        /// Calculates the token probabilities given a class.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="sets">The sets.</param>
        /// <param name="alpha">Additive smoothing parameter. If set to zero, no Laplace smoothing will be applied.</param>
        /// <param name="totalProbability">The total probability for the given classes.</param>
        /// <returns>IEnumerable&lt;ConditionalProbability&lt;IClass, IToken&gt;&gt;.</returns>
        private IEnumerable<ConditionalProbability> CalculateTokenProbabilityGivenClass(IToken token, IEnumerable<IDataSetAccessor> sets, out double totalProbability, double alpha)
        {
            var probabilities = CalculateTokenProbabilityGivenClass(token, sets, alpha).ToCollection();
            totalProbability = probabilities.Sum(p => p.Probability);
            return probabilities;
        }

        /// <summary>
        /// Splits the data sets.
        /// </summary>
        /// <param name="classUnderTest">The class under test.</param>
        /// <param name="remainingSets">The remaining sets.</param>
        /// <returns>IDataSet&lt;IClass, IToken&gt;.</returns>
        private IDataSetAccessor SplitDataSets(IClass classUnderTest, out ICollection<IDataSetAccessor> remainingSets)
        {
            IDataSet setForClassUnderTest = null;
            remainingSets = new Collection<IDataSetAccessor>();

            // split data sets by selected class and other classes
            foreach (var trainingSet in _trainingSets)
            {
                // select the set for the class under test
                if (trainingSet.Class.Equals(classUnderTest))
                {
                    Debug.Assert(setForClassUnderTest == null,
                        "The class under test must not have multiple sets registered in the DataSet");
                    setForClassUnderTest = trainingSet;
                    continue;
                }

                // select remaining sets
                remainingSets.Add(trainingSet);
            }

            // return the found set or an empty set
            return setForClassUnderTest ?? new EmptyDataSet(classUnderTest);
        }
    }
}
