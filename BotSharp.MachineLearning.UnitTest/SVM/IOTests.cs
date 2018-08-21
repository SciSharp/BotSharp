using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SVM;
using System.Linq;
using System.IO;
using System.Text;
using SVM.BotSharp.MachineLearning;
using SVM.BotSharp.MachineLearningTest;

namespace BotSharp.MachineLearning.UnitTest.SVM
{
    [TestClass]
    public class IOTests
    {
        [TestMethod]
        public void ReadProblem()
        {
            Problem expected = SVMUtilities.CreateTwoClassProblem(100);
            Problem actual = Problem.Read("train0.problem");

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void WriteProblem()
        {
            Problem prob = SVMUtilities.CreateTwoClassProblem(100);
            using (MemoryStream stream = new MemoryStream())
            using (StreamReader input = new StreamReader("train0.problem"))
            {
                Problem.Write(stream, prob);
                string expected = input.ReadToEnd().Replace("\r\n", "\n");
                string actual = Encoding.ASCII.GetString(stream.ToArray());
                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void ReadModel()
        {
            Problem train = SVMUtilities.CreateTwoClassProblem(100);
            Parameter param = new Parameter();
            RangeTransform transform = RangeTransform.Compute(train);
            Problem scaled = transform.Scale(train);
            param.KernelType = KernelType.LINEAR;

            Training.SetRandomSeed(SVMUtilities.TRAINING_SEED);
            Model expected = Training.Train(scaled, param);
            Model actual = Model.Read("svm0.model");

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void WriteModel()
        {
            Problem train = SVMUtilities.CreateTwoClassProblem(100);
            Parameter param = new Parameter();
            RangeTransform transform = RangeTransform.Compute(train);
            Problem scaled = transform.Scale(train);
            param.KernelType = KernelType.LINEAR;

            Training.SetRandomSeed(SVMUtilities.TRAINING_SEED);
            Model model = Training.Train(scaled, param);

            using (MemoryStream stream = new MemoryStream())
            using (StreamReader input = new StreamReader("svm0.model"))
            {
                Model.Write(stream, model);
                string expected = input.ReadToEnd().Replace("\r\n", "\n");
                string actual = Encoding.ASCII.GetString(stream.ToArray());
                Assert.AreEqual(expected, actual);
            }
        }
    }
}
