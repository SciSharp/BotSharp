using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.Classifiers
{
    public class FasttextClassifier : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }

        public PipeSettings Settings { get; set; }

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            string modelFileName = Path.Join(Settings.ModelDir, meta.Model);
            string predictFileName = Path.Join(Settings.PredictDir, "fasttext.txt");
            File.WriteAllText(predictFileName, doc.Sentences[0].Text);

            var output = Engines.Classifiers.CmdHelper.Run(Path.Join(Settings.AlgorithmDir, "fasttext"), $"predict-prob {modelFileName}.bin {predictFileName}");

            File.Delete(predictFileName);

            doc.Sentences[0].Intent = new TextClassificationResult
            {
                Label = output.Split(' ')[0].Split("__label__")[1],
                Confidence = decimal.Parse(output.Split(' ')[1])
            };

            return true;
        }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            meta.Model = "classification-fasttext.model";

            string parsedTrainingDataFileName = Path.Join(Settings.TrainDir, $"classification-fasttext.parsed.txt");
            string modelFileName = Path.Join(Settings.ModelDir, meta.Model);

            // assemble corpus
            StringBuilder corpus = new StringBuilder();
            agent.Corpus.UserSays.ForEach(x => corpus.AppendLine($"__label__{x.Intent} {x.Text}"));

            File.WriteAllText(parsedTrainingDataFileName, corpus.ToString());

            var output = Engines.Classifiers.CmdHelper.Run(Path.Join(Settings.AlgorithmDir, "fasttext"), $"supervised -input {parsedTrainingDataFileName} -output {modelFileName}", false);

            Console.WriteLine($"Saved model to {modelFileName}");
            meta.Meta = new JObject();
            meta.Meta["compiled at"] = "Aug 3, 2018";


            return true;
        }
    }

    public static class CmdHelper
    {
        public static string Run(string fileName, string arguments, bool outputAsync = true)
        {
            Console.WriteLine($"{fileName} {arguments}");

            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            ProcessStartInfo procStartInfo = new ProcessStartInfo(fileName);
            // procStartInfo.Arguments = arguments;
            // The following commands are needed to redirect the standard output.
            // This means that it will be redirected to the Process.StandardOutput StreamReader.
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;
            procStartInfo.UseShellExecute = false;
            // Do not create the black window.
            //procStartInfo.CreateNoWindow = true;
            if (procStartInfo.EnvironmentVariables.ContainsKey("OS") && procStartInfo.EnvironmentVariables["OS"] == "Windows_NT")
            {
                procStartInfo.FileName = fileName + ".exe";
            }
            else
            {
                procStartInfo.FileName = "sh";
                procStartInfo.RedirectStandardInput = true;
                procStartInfo.CreateNoWindow = false;
            }
            proc.StartInfo = procStartInfo;

            string output = String.Empty;

            proc.Start();
            if (procStartInfo.EnvironmentVariables.ContainsKey("OS") && procStartInfo.EnvironmentVariables["OS"] == "Windows_NT")
            {
            
            }
            else
            {
                proc.StandardInput.WriteLine($"{fileName} {arguments}" + "&exit");
                proc.StandardInput.AutoFlush = false;
            }

            using (StreamReader reader = proc.StandardOutput)
            {
                if (outputAsync)
                {
                    string buffer = String.Empty;
                    while (!proc.HasExited)
                    {
                        Thread.Sleep(1);
                        buffer = proc.StandardOutput.ReadLine();
                        output += buffer;
                        Console.WriteLine(buffer);
                    }
                }
                else
                {
                    output = reader.ReadToEnd();
                    Console.WriteLine(output);
                }
            }
            
            proc.WaitForExit();
            proc.Close();

            return output;
        }
    }
}
