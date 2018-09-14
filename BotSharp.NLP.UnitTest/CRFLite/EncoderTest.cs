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

        object rdLocker = new object();

        [TestMethod]
        public void TestDecode()
        {
            var decoder = new CRFDecoder();
            var options = new DecoderOptions
            {
                /*
                 * input data format
                 * 
                    In IN
                    its	PRP$
                    sixth JJ
                    edition	NN
                    , PUN
                    the	DT
                    Beijing	NNP
                    . PUN
                 */
                InputFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\test.txt",
                ModelFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\ner_model"
            };

            var sr = new StreamReader(options.InputFileName);

            //Load encoded model from file
            decoder.LoadModel(options.ModelFileName);

            var parallelOption = new ParallelOptions();
            parallelOption.MaxDegreeOfParallelism = options.Thread;
            Parallel.For(0, options.Thread, parallelOption, t =>
            {

                //Create decoder tagger instance. If the running environment is multi-threads, each thread needs a separated instance
                var tagger = decoder.CreateTagger(options.NBest, options.MaxWord);
                tagger.set_vlevel(options.ProbLevel);

                //Initialize result
                var crf_out = new crf_seg_out[options.NBest];
                for (var i = 0; i < options.NBest; i++)
                {
                    crf_out[i] = new crf_seg_out(tagger.crf_max_word_num);
                }

                var inbuf = new List<List<string>>();

                inbuf.Add(new List<string>
                {
                    "'	PUN",
                    "'	POS",
                    "Duchy	NNP",
                    "of	IN",
                    "Lithuania	NNP"
                });

                while (true)
                {
                    lock (rdLocker)
                    {
                        if (ReadRecord(inbuf, sr) == false)
                        {
                            break;
                        }
                    }

                    //Call CRFSharp wrapper to predict given string's tags
                    decoder.Segment((CRFTermOut[])crf_out, (DecoderTagger)tagger, inbuf);
                }
            });


            sr.Close();
        }

        private bool ReadRecord(List<List<string>> inbuf, StreamReader sr)
        {
            inbuf.Clear();

            while (true)
            {
                var strLine = sr.ReadLine();
                if (strLine == null)
                {
                    //At the end of current file
                    if (inbuf.Count == 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                strLine = strLine.Trim();
                if (strLine.Length == 0)
                {
                    return true;
                }

                //Read feature set for each record
                var items = strLine.Split(new char[] { '\t' });
                inbuf.Add(new List<string>());
                for (int index = 0; index < items.Length; index++)
                {
                    var item = items[index];
                    inbuf[inbuf.Count - 1].Add(item);
                }
            }
        }
    }
}
