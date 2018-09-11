using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SVM;
using SVM.BotSharp.MachineLearning;
using SVM.BotSharp.MachineLearningTest;

namespace BotSharp.NLP.UnitTest.SVM
{
    [TestClass]
    public class RegressionTests
    {
        [TestMethod]
        public void TestRegression()
        {
            SvmType[] svmTypes = new SvmType[] { SvmType.NU_SVR, SvmType.EPSILON_SVR };
            // LINEAR kernel is pretty horrible for regression
            KernelType[] kernelTypes = new KernelType[] { KernelType.LINEAR, KernelType.RBF, KernelType.SIGMOID };

            foreach (SvmType svm in svmTypes)
            {
                foreach (KernelType kernel in kernelTypes)
                {
                    double error = testRegressionModel(100, svm, kernel);

                    Assert.AreEqual(0, error, 2, string.Format("SVM {0} with Kernel {1} did not train correctly", svm, kernel));
                }
            }
        }

        private double testRegressionModel(int count, SvmType svm, KernelType kernel, string outputFile = null)
        {
            Problem train = SVMUtilities.CreateRegressionProblem(count);
            Parameter param = new Parameter();
            RangeTransform transform = RangeTransform.Compute(train);
            Problem scaled = transform.Scale(train);
            param.Gamma = 1.0 / 2;
            param.SvmType = svm;
            param.KernelType = kernel;
            param.Degree = 2;

            Model model = Training.Train(scaled, param);

            Problem test = SVMUtilities.CreateRegressionProblem(count, false);
            scaled = transform.Scale(test);
            return Prediction.Predict(scaled, outputFile, model, false);
        }
    }
}
