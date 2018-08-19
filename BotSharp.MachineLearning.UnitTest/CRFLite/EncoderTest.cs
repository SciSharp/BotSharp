using BotSharp.MachineLearning.CRFLite;
using BotSharp.MachineLearning.CRFLite.Encoder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BotSharp.MachineLearning.UnitTest.CRFLite
{
    [TestClass]
    public class EncoderTest
    {
        [TestMethod]
        public void TestEncode()
        {
            var encoder = new CRFEncoder();
            bool result = encoder.Learn(new EncoderOptions
            {
                TrainingCorpusFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\English\corpus\eng.1K.training",
                TemplateFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\English\template.NE",
                ModelFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\English\model\ner_model_eng"
            });
        }
    }
}
