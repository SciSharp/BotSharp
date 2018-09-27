using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// Hypothesis testing framework:
    /// 1. Start with a null hypothesis (null_value) that represents the status quo (i.e. common believe).
    /// 2. Create an alternative hypothesis (H_A) that represents the research questions, i.e. what is being tested for.
    /// 3. Conduct a hypothesis test under the assumption that the null hypothesis is true, either via simulation or theoretical methods (i.e. methods that rely on Central Limit Theorem)
    /// 4. If thte test results suggest that the data do not provide convincing evidence for the alternative hypothesis, we stick with the null hypothesis. If the do, then we reject 
    /// the null hyptothesis in favor of the alternative
    /// 
    /// p-value = P(observed or more extreme outcome | null_value true)
    /// p-value is the probability of observing data at least as favorable to the alternative hypothesis as our current data set, if the null hyptothesis was true
    /// 1. If the p-value is low (lower than the significance level, alpha, which is usually 5%) we say the it would be very unlikely to observe the data if the null hypothesis were true, and hence reject H_0
    /// 2. If the p-value is high (higher than alpha) we say that is likely to observe the data even if the null hypothesis were true, and hence do not reject H_0
    /// 
    /// Steps for Hypothesis testing for a single mean:
    /// 1. Set the hypotheses: H_0: mu = null value
    ///                        H_A: mu < or > or != null value
    /// 2. Calculate the point estimate (usually the sample mean)
    /// 3. Check CLT (i.e., Central Limit Theorem) condictions:
    ///   I. Independence: Sampled observations must be independent (random sample/assignment & if sampling without replacement, n < 10% of population)
    ///   II. Sample size/skew: n>=30, larger if the population distribution is very skewed
    /// 4. Draw sample distribution, shade p-value, calculate test statistics Z = (point_estimate - null_value) / SE
    /// 5. Make a decision, and interpret it in context of the research question:
    ///   if p-value < alpha, reject H_0; the data provide convincing evidence for H_A
    ///   if p-value > alpha, do not reject H_0; the data do not provide convincing evidence for H_A
    ///   
    /// There are two types of decision errors in hypothesis testing:
    /// Type I error is rejecting H_0 when H_0 is true
    /// Type II error is failing to reject H_0 when H_A is true
    /// (Usually from a social point of view, Type I error is the worst error as it means rejecting something that is true and status quo, but this depends on scenario and personal preferences)
    /// By definition:
    /// P(Type I error | H_0 is true) = alpha, where alpha is the significance_level => prefer small significance level, increasing significance level increases the Type I error rate.
    /// On the other hand, decreasing significance_level increases the Type II error
    /// 
    /// The power of a test is the probability of correctly reject H_0, the probability of doing so is (1-beta), where beta = P(Type II error | H_A is true)
    /// beta depends on the effect size, which is the difference between point estimate and the null_value => if the effect size is large, then beta is small
    /// 
    /// Real differences between the point estimate and null_value are easier to detect with larger sample. However, very large samples will result in a statistical significance even
    /// for tiny differences between sample mean (i.e. point estimate) and the null value (the difference refers to the effect size), even when the difference is not practically significant.
    /// 
    /// Note that in case the sample size is smaller than 30, the CLT does not hold and Student's t distribution should be used for the population statistics instead of normal distribution
    /// </summary>
    public class HypothesisTesting
    {
        /// <summary>
        /// Two-sided or one-sided test for a single mean
        /// 
        /// A two-sided hypothesis with threshold of alpha (i.e. significance level) is equivalent to a confidence interval of CL (i.e. confidence level) = 1 - alpha
        /// A one-sided hypothesis with threshold of alpha is equivalent to a confidence interval of CL = 1 - alpha * 2
        /// 
        /// If H_0 is rejected, a confidence interval that agrees with the result of the hypothesis test should not include the null_value
        /// If H_0 is failed to be rejected, a confidence interval that agrees with the result of the hypothesis should include the null_value
        /// </summary>
        /// <param name="values">value sample for the varabiel</param>
        /// <param name="null_value">The null hypothesis value that true population mean, mu = null_value</param>
        /// <param name="significance_level"></param>
        /// <param name="one_sided">True if the test is one-sided</param>
        /// <returns>True if the null hypothesis H_0 : (mu == null_value) is rejected</returns>
        public bool RejectH0_ByCI(double[] values, double null_value, double significance_level = 0.05, bool one_sided = false)
        {
            double confidence_level = 1 - significance_level * (one_sided ? 2 : 1);

            double[] confidence_interval = ConfidenceInterval.GetConfidenceInterval(values, confidence_level);
            return null_value < confidence_interval[0] || null_value > confidence_interval[1];
        }

        /// <summary>
        /// Two-sided or one-sided test for a single mean
        /// 
        /// Given that:
        /// H_0 : mu = null_value
        /// H_A : mu != null_value
        /// 
        /// By Central Limit Theorem:
        /// sample_mean ~ N(mu, SE)
        /// 
        /// p-value = (sample_mean is at least ||null_value-point_estimate|| away from the null_value) | mu = null_value)
        /// if(p-value < significance_level) reject H_0
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="null_value"></param>
        /// <param name="significance_level"></param>
        /// <param name="one_sided">True if the test is one_sided</param>
        /// <returns></returns>
        public static bool RejectH0(double[] sample, double null_value, out double pValue, double significance_level = 0.05, bool one_sided = false, bool useStudentT = false)
        {
            double pointEstimate = Mean.GetMean(sample);
            double standardError = StandardError.GetStandardError(sample); //SE is the estimated standard deviation of the true population mean, mu
            double test_statistic = System.Math.Abs(pointEstimate - null_value) / standardError; //This assumes that H_0 is true, that is, the true population mean, mu = null_value

            double percentile = 0;
            if (sample.Length < 30 || useStudentT) //if sample size is smaller than 30, then CLT for population statistics such as sample mean no longer holds and Student's t distribution should be used in place of the normal distribution
            {
                percentile = StudentT.GetPercentile(test_statistic, sample.Length - 1);
            }
            else
            {
                percentile = Gaussian.GetPercentile(test_statistic);
            }

            pValue = pValue = (1 - percentile) * (one_sided ? 1 : 2);
            return pValue < significance_level;
        }

        /// <summary>
        /// Two-sided or one-sided test for a single statistic
        /// 
        /// Given that:
        /// H_0 : mu = null_value
        /// H_A : mu != null_value
        /// 
        /// By Central Limit Theorem:
        /// sample_mean ~ N(mu, SE)
        /// 
        /// p-value = (sample_mean is at least ||null_value-point_estimate|| away from the null_value) | mu = null_value)
        /// if(p-value < significance_level) reject H_0
        /// </summary>
        /// <param name="point_estimate">point estimate of the population statistics (e.g., sample mean, sample median, etc.)</param>
        /// <param name="null_value"></param>
        /// <param name="SE">standard error of the population statistics</param>
        /// <param name="significance_level"></param>
        /// <param name="one_sided"></param>
        /// <returns></returns>
        public static bool RejectH0(double point_estimate, double null_value, double SE, int sampleSize, out double pValue, double significance_level = 0.05, bool one_sided = false, bool useStudentT = false)
        {
            double test_statistic = System.Math.Abs(point_estimate - null_value) / SE; //This assumes that H_0 is true, that is, the true population mean, mu = null_value

            double percentile = 0;
            if (sampleSize < 30 || useStudentT) //if sample size is smaller than 30, then CLT for population statistics such as sample mean no longer holds and Student's t distribution should be used in place of the normal distribution
            {
                percentile = StudentT.GetPercentile(test_statistic, sampleSize - 1);
            }
            else
            {
                percentile = Gaussian.GetPercentile(test_statistic);
            }

            pValue = (1 - percentile) * (one_sided ? 1 : 2);
            return pValue < significance_level;
        }

        /// <summary>
        /// Two-sided test for whether statitics of two variables are equal in the true population, var1 and var2 are independent
        /// 
        /// Hypotheses are:
        /// H_0: mu_var1 = mu_var2
        /// H_1: mu_var1 != mu_var2
        /// 
        /// The hypotheses can be written as 
        /// H_0: mu_var1 - mu_var2 = 0
        /// H_1: mu_var1 - mu_var2 != 0
        /// 
        /// By Central Limt Theorem:
        /// sample_mean_var1 - sample_mean_var2 ~ N(0, SE), where null_value = 0 and SE is the standard error of the sampling distribution
        /// </summary>
        /// <param name="sample_for_var1">value sample for variable 1</param>
        /// <param name="sample_for_var2">value sample for variable 2</param>
        /// <param name="significance_level"></param>
        /// <returns></returns>
        public bool RejectH0_ByCI(double[] sample_for_var1, double[] sample_for_var2, double significance_level = 0.05, bool one_sided = false)
        {
            double confidence_level = 1 - significance_level * (one_sided ? 2 : 1);
            double[] confidence_interval = ConfidenceInterval.GetConfidenceIntervalForDiff(sample_for_var1, sample_for_var2, confidence_level);
            double null_value = 0;
            return null_value < confidence_interval[0] || null_value > confidence_interval[1];
        }

        /// <summary>
        /// Two-sided or one-sided test for whether statitics of two variables are equal in the true population, var1 and var2 are paired and dependent
        /// 
        /// Hypotheses are:
        /// H_0: mu_var1 = mu_var2
        /// H_1: mu_var1 != mu_var2
        /// 
        /// The hypotheses can be written as 
        /// H_0: mu_var1 - mu_var2 = 0
        /// H_1: mu_var1 - mu_var2 != 0
        /// 
        /// By Central Limt Theorem:
        /// sample_mean_var1 - sample_mean_var2 ~ N(0, SE), where null_value = 0 and SE is the standard error of the sampling distribution
        /// </summary>
        /// <param name="sample_for_paired_data">a random sample consisting data paired together, var1 and var2, var1 and var2 are not independent</param>
        /// <param name="one_sided">True if the test is one-sided</param>
        /// <param name="significance_level"></param>
        /// <returns></returns>
        public bool RejectH0_PairedData_ByCI(Tuple<double, double>[] sample_for_paired_data, double significance_level = 0.05, bool one_sided = false)
        {
            int sample_size = sample_for_paired_data.Length;
            double[] diff = new double[sample_size];
            for (int i = 0; i < sample_size; ++i)
            {
                diff[i] = sample_for_paired_data[i].Item1 - sample_for_paired_data[i].Item2;
            }
            double confidence_level = 1 - significance_level * (one_sided ? 2 : 1);
            double[] confidence_interval = ConfidenceInterval.GetConfidenceInterval(diff, confidence_level);
            double null_value = 0;
            return null_value < confidence_interval[0] || null_value > confidence_interval[1];
        }


        /// <summary>
        /// Two-sided or one-sided test for whether statitics of two variables are equal in the true population, var1 and var2 are independent
        /// 
        /// Hypotheses are:
        /// H_0: mu_var1 = mu_var2
        /// H_1: mu_var1 != mu_var2
        /// 
        /// The hypotheses can be written as 
        /// H_0: mu_var1 - mu_var2 = 0
        /// H_1: mu_var1 - mu_var2 != 0
        /// 
        /// By Central Limt Theorem:
        /// sample_mean_var1 - sample_mean_var2 ~ N(0, SE), where null_value = 0 and SE is the standard error of the sampling distribution
        /// 
        /// p-value = (sample_mean is at least ||null_value-point_estimate|| away from the null_value) | mu = null_value)
        /// </summary>
        /// <param name="sample_for_var1">value sample for variable 1</param>
        /// <param name="sample_for_var2">value sample for variable 2</param>
        /// <param name="one_sided">True if the test is one-sided</param>
        /// <param name="significance_level"></param>
        /// <returns></returns>
        public bool RejectH0(double[] sample_for_var1, double[] sample_for_var2, out double pValue, double significance_level = 0.05, bool one_sided = false, bool useStudentT = false)
        {
            double pointEstimate = Mean.GetMean(sample_for_var1) - Mean.GetMean(sample_for_var2);
            double null_value = 0;
            double SE = StandardError.GetStandardError(sample_for_var1, sample_for_var2);
            double test_statistic = System.Math.Abs(pointEstimate - null_value) / SE;

            double percentile = 0;
            if (sample_for_var1.Length < 30 || sample_for_var2.Length < 30 || useStudentT) //if sample size is smaller than 30, then CLT for population statistics such as sample mean no longer holds and Student's t distribution should be used in place of the normal distribution
            {
                int df = System.Math.Min(sample_for_var1.Length - 1, sample_for_var2.Length - 1);
                percentile = StudentT.GetPercentile(test_statistic, df);
            }
            else
            {
                percentile = Gaussian.GetPercentile(test_statistic);
            }

            pValue = (1 - percentile) * (one_sided ? 1 : 2);
            return pValue < significance_level;
        }

        /// <summary>
        /// Two-sided or one-sided test for whether statitics of two variables are equal in the true population, var1 and var2 are paired and dependent
        /// 
        /// Hypotheses are:
        /// H_0: mu_var1 = mu_var2
        /// H_1: mu_var1 != mu_var2
        /// 
        /// The hypotheses can be written as 
        /// H_0: mu_var1 - mu_var2 = 0
        /// H_1: mu_var1 - mu_var2 != 0
        /// 
        /// By Central Limt Theorem:
        /// sample_mean_var1 - sample_mean_var2 ~ N(0, SE), where null_value = 0 and SE is the standard error of the sampling distribution
        /// 
        /// p-value = (sample_mean is at least ||null_value-point_estimate|| away from the null_value) | mu = null_value)
        /// </summary>
        /// <param name="sample_for_paired_data">a random sample consisting data paired together, var1 and var2, var1 and var2 are not independent</param>
        /// <param name="one_sided">True if the test is one-sided</param>
        /// <param name="significance_level"></param>
        /// <returns></returns>
        public bool RejectH0_PairedData(Tuple<double, double>[] sample_for_paired_data, out double pValue, double significance_level = 0.05, bool one_sided = false, bool useStudentT = false)
        {
            int sample_size = sample_for_paired_data.Length;
            double[] diff = new double[sample_size];
            for (int i = 0; i < sample_size; ++i)
            {
                diff[i] = sample_for_paired_data[i].Item1 - sample_for_paired_data[i].Item2;
            }
            double point_estimate = Mean.GetMean(diff);
            double null_value = 0;
            double SE = StandardError.GetStandardError(diff);
            double test_statistic = System.Math.Abs(point_estimate - null_value) / SE;

            double percentile = 0;
            if (sample_for_paired_data.Length < 30 || useStudentT) //if sample size is smaller than 30, then CLT for population statistics such as sample mean no longer holds and Student's t distribution should be used in place of the normal distribution
            {
                percentile = StudentT.GetPercentile(test_statistic, sample_for_paired_data.Length - 1);
            }
            else
            {
                percentile = Gaussian.GetPercentile(test_statistic);
            }

            pValue = (1 - percentile) * (one_sided ? 1 : 2);
            return pValue < significance_level;
        }

        /// <summary>
        /// Check whether variable 1 is truely greater than variable 2 at 0.95 statistical significance confidence level, var1 and var2 are independent
        /// </summary>
        /// <param name="sample_for_var1">value sample for variable 1</param>
        /// <param name="sample_for_var2">value sample for variable 2</param>
        /// <param name="confidence_level"></param>
        /// <returns></returns>
        public bool AreGreater(double[] sample_for_var1, double[] sample_for_var2, double confidence_level = 0.95)
        {
            double[] confidence_interval_for_var1 = ConfidenceInterval.GetConfidenceInterval(sample_for_var1, confidence_level);
            double[] confidence_interval_for_var2 = ConfidenceInterval.GetConfidenceInterval(sample_for_var2, confidence_level);
            return confidence_interval_for_var1[0] > confidence_interval_for_var2[1];
        }

        /// <summary>
        /// Check whether variable 1 is truely less than variable 2 at 0.95 statistical significance confidence level
        /// </summary>
        /// <param name="sample_for_var1">value sample for variable 1</param>
        /// <param name="sample_for_var2">value sample for variable 2</param>
        /// <param name="confidence_level"></param>
        /// <returns></returns>
        public bool AreLessThan(double[] sample_for_var1, double[] sample_for_var2, double confidence_level = 0.95)
        {
            double[] confidence_interval_for_var1 = ConfidenceInterval.GetConfidenceInterval(sample_for_var1, confidence_level);
            double[] confidence_interval_for_var2 = ConfidenceInterval.GetConfidenceInterval(sample_for_var2, confidence_level);
            return confidence_interval_for_var1[1] < confidence_interval_for_var2[0];
        }
    }
}
