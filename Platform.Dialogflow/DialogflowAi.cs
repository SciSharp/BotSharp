using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Core;
using BotSharp.Core.Engines;
using BotSharp.NLP;
using BotSharp.Platform.Abstraction;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.AiRequest;
using BotSharp.Platform.Models.AiResponse;
using DotNetToolkit;
using Platform.Dialogflow.Models;

namespace Platform.Dialogflow
{
    public class DialogflowAi<TAgent> :
        PlatformBuilderBase<TAgent>,
        IPlatformBuilder<TAgent>
        where TAgent : AgentModel
    {
        public TrainingCorpus ExtractorCorpus(TAgent agent)
        {
            var corpus = new TrainingCorpus
            {
                Entities = new List<TrainingEntity>(),
                UserSays = new List<TrainingIntentExpression<TrainingIntentExpressionPart>>()
            };

            agent.Entities.ForEach(entity =>
            {
                corpus.Entities.Add(new TrainingEntity
                {
                    Entity = entity.Name,
                    Values = entity.Entries.Select(x => new TrainingEntitySynonym
                    {
                        Value = x.Value,
                        Synonyms = x.Synonyms.Select(y => y.Synonym).ToList()
                    }).ToList()
                });
            });

            agent.Intents.ForEach(intent =>
            {
                intent.UserSays.ForEach(say => {
                    corpus.UserSays.Add(new TrainingIntentExpression<TrainingIntentExpressionPart>
                    {
                        Intent = intent.Name,
                        Text = String.Join("", say.Data.Select(x => x.Text)),
                        Entities = say.Data.Where(x => !String.IsNullOrEmpty(x.Meta))
                        .Select(x => new TrainingIntentExpressionPart
                        {
                            Value = x.Text,
                            Entity = x.Meta,
                            Start = x.Start
                        })
                        .ToList()
                    });
                });
            });

            return corpus;
        }

        public AiResponse TextRequest(AiRequest request)
        {
            var dataService = new AIDataService(new AIConfiguration("TOKEN", SupportedLanguage.English)
            {
                AgentId = request.AgentId,
                Language = SupportedLanguage.English,
                SessionId = request.SessionId
            });

            var response = dataService.Request(new AIRequest
            {
                SessionId = request.SessionId,
                Query = new string[] { request.Text }
            });

            return response.ToObject<AiResponse>();
        }

        public async Task<bool> Train(TAgent agent, TrainingCorpus corpus)
        {
            var trainer = new BotTrainer();

            var trainOptions = new BotTrainOptions
            {
                //AgentDir = projectPath,
                //Model = model
            };

            var info = await trainer.Train(agent, trainOptions);

            return true;
        }

        protected float[] TrimSilence(float[] samples)
        {
            if (samples == null)
            {
                return null;
            }

            const float min = 0.000001f;

            var startIndex = 0;
            var endIndex = samples.Length;

            for (var i = 0; i < samples.Length; i++)
            {

                if (Math.Abs(samples[i]) > min)
                {
                    startIndex = i;
                    break;
                }
            }

            for (var i = samples.Length - 1; i > 0; i--)
            {
                if (Math.Abs(samples[i]) > min)
                {
                    endIndex = i;
                    break;
                }
            }

            if (endIndex <= startIndex)
            {
                return null;
            }

            var result = new float[endIndex - startIndex];
            Array.Copy(samples, startIndex, result, 0, endIndex - startIndex);
            return result;

        }

        protected static byte[] ConvertArrayShortToBytes(short[] array)
        {
            var numArray = new byte[array.Length * 2];
            Buffer.BlockCopy(array, 0, numArray, 0, numArray.Length);
            return numArray;
        }

        protected static short[] ConvertIeeeToPcm16(float[] source)
        {
            var resultBuffer = new short[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var f = source[i] * 32768f;

                if (f > (double)short.MaxValue)
                    f = short.MaxValue;
                else if (f < (double)short.MinValue)
                    f = short.MinValue;
                resultBuffer[i] = Convert.ToInt16(f);
            }

            return resultBuffer;
        }
    }
}
