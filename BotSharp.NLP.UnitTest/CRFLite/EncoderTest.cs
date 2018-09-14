using BotSharp.Models.CRFLite;
using BotSharp.Models.CRFLite.Decoder;
using BotSharp.Models.CRFLite.Encoder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.NLP.UnitTest.CRFLite
{
    [TestClass]
    public class EncoderTest
    {
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void TestEncode()
        {
            var encoder = new CRFEncoder();
            bool result = encoder.Learn(new EncoderOptions
            {
                /*
                 * traing corups format, split by tab, sentences is seperated by blank row
                 * 
                    ! PUN S
                    Tokyo NNP S_LOCATION
                    and	CC S
                    New	NNP	B_LOCATION
                    York NNP	E_LOCATION
                    are	VBP	S
                    major JJ S
                    financial JJ S
                    centers	NNS	S
                    . PUN S
                 */
                TrainingCorpusFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\eng.1k.training",
                TemplateFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\template.en",
                ModelFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\ner_model"
            });

            Assert.IsTrue(result);
        }
    }
}
