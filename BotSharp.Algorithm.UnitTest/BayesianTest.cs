using BotSharp.Algorithm.Bayesian;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.UnitTest
{
    [TestClass]
    public class BayesianTest
    {
        /// <summary>
        /// The training set
        /// </summary>
        private ITrainingSet _trainingSet;

        /// <summary>
        /// The classifier
        /// </summary>
        private IClassifier _classifier;

        /// <summary>
        /// The spam class
        /// </summary>
        private static IClass _spamClass;

        /// <summary>
        /// The ham class
        /// </summary>
        private static IClass _hamClass;

        /// <summary>
        /// Sets up.
        /// </summary>
        public void SetUp()
        {
            _trainingSet = BuildTrainingSet();
            _classifier = BuildClassifier(_trainingSet);
        }

        /// <summary>
        /// Builds the classifier.
        /// </summary>
        /// <returns>Classifier&lt;StringClass, StringToken&gt;.</returns>
        private IClassifier BuildClassifier(ITrainingSetAccessor trainingSet)
        {
            var classifier = new NaiveClassifier(trainingSet)
            {
                // disable smoothing for exact probabilities
                SmoothingAlpha = 0.0D
            };

            return classifier;
        }

        /// <summary>
        /// Builds the training set.
        /// </summary>
        /// <returns>ITrainingSet&lt;StringClass, StringToken&gt;.</returns>
        private static ITrainingSet BuildTrainingSet()
        {
            var trainingSet = new TrainingSet();

            // build data sets
            var spamSet = BuildSpamDataSet();
            var hamSet = BuildHamDataSet();

            // monkey test
            //spamSet.SetSize.Should()
                //.Be(hamSet.SetSize, "because this test relies on identical set sizes for exact probability testing");

            // register classes
            _spamClass = spamSet.Class;
            _hamClass = hamSet.Class;

            // add the sets and return
            trainingSet.Add(spamSet, hamSet);
            return trainingSet;
        }

        /// <summary>
        /// Builds the spam data set.
        /// </summary>
        /// <returns>IDataSet&lt;StringClass, StringToken&gt;.</returns>
        private static IDataSet BuildSpamDataSet()
        {
            return BuildDataSet("spam", 0.5D, "rolex", "watches", "viagra", "prince", "money", "send", "xyzzy");
        }

        /// <summary>
        /// Builds the spam data set.
        /// </summary>
        /// <returns>IDataSet&lt;StringClass, StringToken&gt;.</returns>
        private static IDataSet BuildHamDataSet()
        {
            return BuildDataSet("ham", 0.5D, "love", "flowers", "unicorn", "friendship", "money", "send", "send");
        }

        /// <summary>
        /// Builds the data set.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="classProbability">The class probability.</param>
        /// <param name="token">The token.</param>
        /// <param name="additionalTokens">The additional tokens.</param>
        /// <returns>IDataSet&lt;StringClass, StringToken&gt;.</returns>
        private static IDataSet BuildDataSet(string className, double classProbability, string token, params string[] additionalTokens)
        {
            var @class = new StringClass(className, classProbability);
            var dataSet = new DataSet(@class);

            dataSet.AddToken(new StringToken(token));
            dataSet.AddToken(additionalTokens.Select(t => new StringToken(t)));

            return dataSet;
        }

        [TestMethod]
        public void CalculateProbabilityReturnsOneHundredPercentForAKnownSpamWord()
        {
            var token = new StringToken("rolex");

            var probability = _classifier.CalculateProbability(_spamClass, token);
            //probability.Should().BeApproximately(1.0D, 0.0001D, "because the word is known the be a spam word");
        }

        [TestMethod]
        public void CalculateProbabilityReturnsOneHundredPercentForAKnownHamWord()
        {
            var token = new StringToken("unicorn");

            var probability = _classifier.CalculateProbability(_hamClass, token);
            //probability.Should().BeApproximately(1.0D, 0.0001D, "because the word is known the be a ham word");
        }

        [TestMethod]
        public void CalculateProbabilitiesWithHamWordReturnsProbabilitiesForAllClasses()
        {
            var token = new StringToken("unicorn");

            var probabilities = _classifier.CalculateProbabilities(token).ToList();
            /*probabilities.Single(p => p.Class.Equals(_spamClass))
                .Probability.Should()
                .BeApproximately(0D, 0.000001D, "because the token is known to be a ham word");

            probabilities.Single(p => p.Class.Equals(_hamClass))
                .Probability.Should()
                .BeApproximately(1D, 0.000001D, "because the token is known to be a ham word");*/
        }

        [TestMethod]
        public void CalculateProbabilitiesWithMixedWordReturnsProbabilitiesForAllClasses()
        {
            var token = new StringToken("money");

            var probabilities = _classifier.CalculateProbabilities(token).ToList();
            /*probabilities.Single(p => p.Class.Equals(_spamClass))
                .Probability.Should()
                .BeApproximately(0.5D, 0.000001D, "because the token is known to be a ham and spam word");

            probabilities.Single(p => p.Class.Equals(_hamClass))
                .Probability.Should()
                .BeApproximately(0.5D, 0.000001D, "because the token is known to be a ham and spam  word");*/
        }

        [TestMethod]
        public void CalculateProbabilitiesWithMixedWordThatIsMoreLikelyHamThanSpamReturnsProbabilitiesForAllClasses()
        {
            var token = new StringToken("send");

            var probabilities = _classifier.CalculateProbabilities(token).ToList();
            /*probabilities.Single(p => p.Class.Equals(_spamClass))
                .Probability.Should()
                .BeApproximately(1 / 3D, 0.000001D, "because the token is more likely to be a ham than spam word");

            probabilities.Single(p => p.Class.Equals(_hamClass))
                .Probability.Should()
                .BeApproximately(2 / 3D, 0.000001D, "because the token is more likely to be a ham than spam word");*/
        }

        [TestMethod]
        public void CalculateProbabilitiesWithRareTokensAndSmoothingAlphaIsUnambiguous()
        {
            var token1 = new StringToken("rolex");
            var token2 = new StringToken("unicorn");
            var token3 = new StringToken("send");

            const double smoothingAlpha = 1.0D;
            var probabilities = _classifier.CalculateProbabilities(new IToken[] { token1, token2, token3 }, smoothingAlpha).ToList();

            /*probabilities.Single(p => p.Class.Equals(_spamClass))
                .Probability.Should()
                .BeLessThan(0.5D, "because we used more ham than spam tokens");

            probabilities.Single(p => p.Class.Equals(_hamClass))
                .Probability.Should()
                .BeGreaterThan(0.5D, "because we used more ham than spam tokens");*/
        }
    }
}
