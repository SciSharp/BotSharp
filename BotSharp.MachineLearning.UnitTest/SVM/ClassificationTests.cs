using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SVM;
using SVM.BotSharp.MachineLearning;
using SVM.BotSharp.MachineLearningTest;

namespace BotSharp.MachineLearning.UnitTest.SVM
{
    [TestClass]
    public class ClassificationTests
    {
        [TestMethod]
        public void TestTwoClass()
        {
            SvmType[] svmTypes = new SvmType[]{SvmType.C_SVC, SvmType.NU_SVC};
            KernelType[] kernelTypes = new KernelType[]{KernelType.LINEAR, KernelType.POLY, KernelType.RBF, KernelType.SIGMOID};

            foreach (SvmType svm in svmTypes)
            {
                foreach (KernelType kernel in kernelTypes)
                {
                    double score = testTwoClassModel(100, svm, kernel);

                    Assert.AreEqual(1, score, .01, string.Format("SVM {0} with Kernel {1} did not train correctly", svm, kernel));
                }
            }
        }

        [TestMethod]
        public void TestTwoClassProbability()
        {
            SvmType[] svmTypes = new SvmType[] { SvmType.C_SVC, SvmType.NU_SVC };
            KernelType[] kernelTypes = new KernelType[] { KernelType.LINEAR, KernelType.POLY, KernelType.RBF, KernelType.SIGMOID };

            foreach (SvmType svm in svmTypes)
            {
                foreach (KernelType kernel in kernelTypes)
                {
                    double score = testTwoClassModel(100, svm, kernel, true);

                    Assert.AreEqual(1, score, .01, string.Format("SVM {0} with Kernel {1} did not train correctly", svm, kernel));
                }
            }
        }

        [TestMethod]
        public void TestMulticlass()
        {
            SvmType[] svmTypes = new SvmType[] { SvmType.C_SVC, SvmType.NU_SVC };
            KernelType[] kernelTypes = new KernelType[] { KernelType.LINEAR, KernelType.POLY, KernelType.RBF, KernelType.SIGMOID };

            foreach (SvmType svm in svmTypes)
            {
                foreach (KernelType kernel in kernelTypes)
                {
                    double score = testMulticlassModel(8, 100, svm, kernel);

                    Assert.AreEqual(1, score, .1, string.Format("SVM {0} with Kernel {1} did not train correctly", svm, kernel));
                }
            }
        }

        [TestMethod]
        public void TestMulticlassProbability()
        {
            SvmType[] svmTypes = new SvmType[] { SvmType.C_SVC, SvmType.NU_SVC };
            KernelType[] kernelTypes = new KernelType[] { KernelType.LINEAR, KernelType.POLY, KernelType.RBF, KernelType.SIGMOID };

            foreach (SvmType svm in svmTypes)
            {
                foreach (KernelType kernel in kernelTypes)
                {
                    double score = testMulticlassModel(8, 100, svm, kernel, true);

                    Assert.AreEqual(1, score, .1, string.Format("SVM {0} with Kernel {1} did not train correctly", svm, kernel));
                }
            }
        }

        private double testTwoClassModel(int count, SvmType svm, KernelType kernel, bool probability = false, string outputFile = null)
        {
            Problem train = SVMUtilities.CreateTwoClassProblem(count);
            Parameter param = new Parameter();
            RangeTransform transform = RangeTransform.Compute(train);
            Problem scaled = transform.Scale(train);
            param.Gamma = .5;
            param.SvmType = svm;
            param.KernelType = kernel;
            param.Probability = probability;
            if (svm == SvmType.C_SVC)
            {
                param.Weights[-1] = 1;
                param.Weights[1] = 1;
            }

            Model model = Training.Train(scaled, param);

            Problem test = SVMUtilities.CreateTwoClassProblem(count, false);
            scaled = transform.Scale(test);
            return Prediction.Predict(scaled, outputFile, model, false);
        }

        private double testMulticlassModel(int numberOfClasses, int count, SvmType svm, KernelType kernel, bool probability = false, string outputFile = null)
        {
            Problem train = SVMUtilities.CreateMulticlassProblem(numberOfClasses, count);
            Parameter param = new Parameter();
            RangeTransform transform = RangeTransform.Compute(train);
            Problem scaled = transform.Scale(train);
            param.Gamma = 1.0 / 3;
            param.SvmType = svm;
            param.KernelType = kernel;
            param.Probability = probability;
            if (svm == SvmType.C_SVC)
            {
                for (int i = 0; i < numberOfClasses; i++)
                    param.Weights[i] = 1;
            }

            Model model = Training.Train(scaled, param);

            Problem test = SVMUtilities.CreateMulticlassProblem(numberOfClasses, count, false);
            scaled = transform.Scale(test);
            return Prediction.Predict(scaled, outputFile, model, false);
        }
    }
}
