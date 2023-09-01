using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Tensorflow;
using static Tensorflow.KerasApi;
using Tensorflow.Keras.Engine;
using Tensorflow.NumPy;
using static Tensorflow.Binding;
using Tensorflow.Keras.Callbacks;
using System.Text.RegularExpressions;
using BotSharp.Plugin.RoutingSpeeder.Settings;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.RoutingSpeeder.Providers.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Tensorflow.Keras;
using BotSharp.Abstraction.Knowledges.Settings;
using System.Numerics;
using Newtonsoft.Json;
using Tensorflow.Keras.Layers;
using BotSharp.Abstraction.Agents;

namespace BotSharp.Plugin.RoutingSpeeder.Providers;

public class IntentClassifier
{
    private readonly IServiceProvider _services;
    Model _model;
    public Model model => _model;
    private bool _isModelReady;
    public bool isModelReady => _isModelReady;
    private ClassifierSetting _settings;

    public IntentClassifier(IServiceProvider services, ClassifierSetting settings)
    {
        _services = services;
        _settings = settings;
    }

    private void Reset()
    {
        keras.backend.clear_session();
        _isModelReady = false;
    }

    private void Build()
    {
        if (_isModelReady)
        {
            return;
        }
        
        var vector = _services.GetRequiredService<ITextEmbedding>();

        var labels = GetLabels();

        var layers = new List<ILayer>
        {
            keras.layers.InputLayer((vector.Dimension), name: "Input"),
            keras.layers.Dense(256, activation:"relu"),
            keras.layers.Dense(256, activation:"relu"),
            keras.layers.Dense(labels.Length, activation: keras.activations.Softmax)
        };
        _model = keras.Sequential(layers);

#if DEBUG
        Console.WriteLine();
        _model.summary();
#endif
        _isModelReady = true;
    }

    private void Fit(NDArray x, NDArray y, TrainingParams trainingParams)
    {
        _model.compile(optimizer: keras.optimizers.Adam(trainingParams.LearningRate),
            loss: keras.losses.SparseCategoricalCrossentropy(),
            metrics: new[] { "accuracy" }
            );

        CallbackParams callback_parameters = new CallbackParams
        {
            Model = _model,
            Epochs = trainingParams.Epochs,
            Verbose = 1,
            Steps = 10
        };

        ICallback earlyStop = new EarlyStopping(callback_parameters, "accuracy");

        var callbacks = new List<ICallback>() { earlyStop };

        var weights = LoadWeights();

        _model.fit(x, y,
            batch_size: trainingParams.BatchSize,
            epochs: trainingParams.Epochs,
            callbacks: callbacks,
            // validation_split: 0.1f,
            shuffle: true);

        _model.save_weights(weights);

        _isModelReady = true;
    }

    public string LoadWeights()
    {
        var agentService = _services.CreateScope().ServiceProvider.GetRequiredService<IAgentService>();

        var weightsFile = Path.Combine(agentService.GetDataDir(), _settings.MODEL_DIR, $"intent-classifier.h5");
        if (File.Exists(weightsFile))
        {
            _model.load_weights(weightsFile);
            _isModelReady = true;
            Console.WriteLine($"Successfully load the weights!");
        }
        else
        {
            Console.WriteLine("No available weights.");
        }
        return weightsFile;
    }

    public (NDArray x, NDArray y) Vectorize(List<DialoguePredictionModel> items)
    {
        var vector = _services.GetRequiredService<ITextEmbedding>();

        var x = np.zeros((items.Count, vector.Dimension), dtype: np.float32);
        var y = np.zeros((items.Count, 1), dtype: np.float32);

        for (int i = 0; i < items.Count; i++)
        {
            x[i] = vector.GetVector(TextClean(items[i].text));
            if (_settings.LabelMappingDict.ContainsKey(items[i].label))
            {
                y[i] = _settings.LabelMappingDict[items[i].label];
            }
        }
        return (x, y);
    }

    public NDArray GetTextEmbedding(string text)
    {
        var knowledgeSettings = _services.GetRequiredService<KnowledgeBaseSettings>();
        var embedding = _services.GetServices<ITextEmbedding>()
            .FirstOrDefault(x => x.GetType().FullName.EndsWith(knowledgeSettings.TextEmbedding));

        var x = np.zeros((1, embedding.Dimension), dtype: np.float32);
        x[0] = embedding.GetVector(text);
        return x;
    }

    public (NDArray, NDArray) PrepareLoadData()
    {
        var agentService = _services.CreateScope().ServiceProvider.GetRequiredService<IAgentService>();
        string rootDirectory = Path.Combine(agentService.GetDataDir(), _settings.RAW_DATA_DIR);
        string saveLabelDirectory = Path.Combine(agentService.GetDataDir(), _settings.MODEL_DIR, _settings.LABEL_FILE_NAME);

        if (!Directory.Exists(rootDirectory))
        {
            throw new Exception($"No training data found! Please put training data in this path: {rootDirectory}");
        }

        var vector = _services.GetRequiredService<ITextEmbedding>();


        var vectorList = new List<float[]>();

        var labelList = new List<string>();
        foreach (var filePath in GetFiles())
        {
            var texts = File.ReadAllLines(filePath, Encoding.UTF8).Select(x => TextClean(x)).ToList();
            vectorList.AddRange(vector.GetVectors(texts));
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            labelList.AddRange(Enumerable.Repeat(fileName, texts.Count).ToList());
        }

        // Write label into local file
        var uniqueLabelList = labelList.Distinct().OrderBy(x => x).ToArray();
        File.WriteAllLines(saveLabelDirectory, uniqueLabelList);

        var x = np.zeros((vectorList.Count, vector.Dimension), dtype: np.float32);
        var y = np.zeros((vectorList.Count, 1), dtype: np.float32);

        for (int i = 0; i < vectorList.Count; i++)
        {
            x[i] = vectorList[i];
            // y[i] = (float)uniqueLabelList.IndexOf(labelList[i]);
            y[i] = (float)Array.IndexOf(uniqueLabelList, labelList[i]);
        }
        return (x, y);
    }

    public string[] GetFiles()
    {
        var agentService = _services.CreateScope().ServiceProvider.GetRequiredService<IAgentService>();
        string rootDirectory = Path.Combine(agentService.GetDataDir(), _settings.RAW_DATA_DIR);
        return Directory.GetFiles(rootDirectory).OrderBy(x => x).ToArray();
    }

    public string[] GetLabels()
    {
        var agentService = _services.CreateScope().ServiceProvider.GetRequiredService<IAgentService>();
        string rootDirectory = Path.Combine(agentService.GetDataDir(), _settings.MODEL_DIR, _settings.LABEL_FILE_NAME);
        var labelText = File.ReadAllLines(rootDirectory);
        return labelText.OrderBy(x => x).ToArray();
    }

    public string TextClean(string text)
    {
        // Remove punctuation
        // Remove digits
        // To lowercase
        var processedText = Regex.Replace(text, "[AB0-9]", " ");
        processedText = string.Join("", processedText.Select(c => char.IsPunctuation(c) ? ' ' : c).ToList());
        processedText = processedText.Replace("  ", " ").ToLower();
        return processedText;
    }

    public string Predict(NDArray vector, float confidenceScore = 0.9f)
    {
        if (!_isModelReady)
        {
            InitClassifer();
        }

        var prob = _model.predict(vector).numpy();

        var probLabel = tf.arg_max(prob, -1).numpy().ToArray<long>();
        prob = np.squeeze(prob, axis: 0);

        if (prob[probLabel[0]] < confidenceScore)
        {
            return string.Empty;
        }

        var prediction = GetLabels()[probLabel[0]];

        return prediction;
    }
    public void InitClassifer()
    {
        Reset();
        Build();
        LoadWeights();
    }

    public void Train()
    {
        var trainingParams = new TrainingParams();
        Reset();
        (var x, var y) = PrepareLoadData();
        Build();
        Fit(x, y, trainingParams);

    }
}
