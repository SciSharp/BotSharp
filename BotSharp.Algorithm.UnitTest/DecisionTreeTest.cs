using BotSharp.Algorithm.DecisionTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace BotSharp.Algorithm.UnitTest
{
    [TestClass]
    public class DecisionTreeTest : TestEssential
    {
        [TestMethod]
        public void TestID3()
        {
            // train
            string dataDir = Configuration.GetSection("dataDir").Value;
            string filePath = Path.Combine(dataDir, "training", "DecisionTree.csv");
            var data = new DataReader().Read(filePath);

            var decisionTree = new Tree();
            decisionTree.Root = Tree.Learn(data, "");

            // predict
            Dictionary<string, string> testValues = new Dictionary<string, string>();
            testValues.Add("Outlook", "Rain");
            testValues.Add("Temperatur", "Mild");
            testValues.Add("Humidity", "High");
            testValues.Add("Wind", "Weak");

            var result = Tree.CalculateResult(decisionTree.Root, testValues, "");

            Tree.Print(null, result);
            Tree.PrintLegend("The colors indicate the following values:");

            Assert.IsTrue(result.Equals("OUTLOOK -- rain --> WIND -- weak --> NO"));
        }
    }
}
