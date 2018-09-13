using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using BotSharp.Core.Engines.BotSharp;
using BotSharp.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BotSharp.Algorithm.Extensions;

namespace BotSharp.Core.UnitTest.Performance
{
    [TestClass]
    public class Spotify : TestEssential
    {
        private List<Tuple<AIRequest, string>> Samples;
        private IBotPlatform _platform;

        [TestMethod]
        public void IntentAccuracy()
        {
            int correct = 0;
            List<Tuple<string, string>> errors = new List<Tuple<string, string>>();

            var agent = LoadAgent();

            for (int i = 0; i < Samples.Count; i++)
            {
                var aIResponse = _platform.TextRequest(Samples[i].Item1);
                if (aIResponse.Result.Metadata.IntentName == Samples[i].Item2)
                {
                    correct++;
                }
                else
                {
                    errors.Add(new Tuple<string, string>(Samples[i].Item2, Samples[i].Item1.Query[0]));
                }
            }

           double accuracy = correct / (Samples.Count + 0.0);
        }

        private Agent LoadAgent()
        {
            _platform = new BotSharpAi();

            // Load agent
            var projectPath = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", "Spotify");
            string model = Directory.GetDirectories(projectPath).Where(x => x.Contains("model_")).Last().Split(Path.DirectorySeparatorChar).Last();
            var modelPath = Path.Combine(projectPath, model);
            var agent = _platform.LoadAgentFromFile(modelPath);

            // Init samples
            Samples = new List<Tuple<AIRequest, string>>();
            /*agent.Corpus.UserSays = new List<TrainingIntentExpression<TrainingIntentExpressionPart>>
            {
                new TrainingIntentExpression<TrainingIntentExpressionPart>{ Intent = "music.play", Text = "play the 50 Great Beatles Songs playlist in Prime Music"},
                new TrainingIntentExpression<TrainingIntentExpressionPart>{ Intent = "music.play", Text = "reproduce a the track Monster by Rihanna ft Eminem"},
                new TrainingIntentExpression<TrainingIntentExpressionPart>{ Intent = "music_player_control.add_favorite", Text = "add this song to my favourites"}
            };*/
            agent.Corpus.UserSays.ForEach(intent =>
            {
                Samples.Add(new Tuple<AIRequest, string>(new AIRequest
                {
                    AgentDir = projectPath,
                    Model = model,
                    Query = new String[]
                    {
                        intent.Text
                    }
                }, intent.Intent));
            });

            //Samples.Shuffle();

            var samples = String.Join("\r\n", Samples.Select(x => $"__label__{x.Item2} {x.Item1.Query[0]}").ToList());

            return agent;
        }
    }
}
