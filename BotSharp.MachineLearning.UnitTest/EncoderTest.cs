using BotSharp.MachineLearning.CRFLite;
using BotSharp.MachineLearning.CRFLite.Encoder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BotSharp.MachineLearning.UnitTest
{
    [TestClass]
    public class EncoderTest
    {
        [TestMethod]
        public void TestEncode()
        {
            var encoder = new CRFEncoder();
            bool bRet = encoder.Learn(new EncoderOptions
            {
                TrainingCorpusFileName = "",
                TemplateFileName = "",
                ModelFileName = ""
            });
        }
    }
}
