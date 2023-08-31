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

        var layers = new List<ILayer>
        {
            keras.layers.InputLayer((300), name: "Input"),
            keras.layers.Dense(256, activation:"relu"),
            keras.layers.Dense(256, activation:"relu"),
            keras.layers.Dense(_settings.LabelMappingDict.Count, activation: keras.activations.Softmax)
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
        var weightsFile = Path.Combine(_settings.MODEL_DIR, $"intent-classifier.h5");
        if (File.Exists(weightsFile))
        {
            _model.load_weights(weightsFile);
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
        var x = np.zeros((items.Count, vector.Dimension), dtype: np.float32);
        var y = np.zeros((items.Count, 1), dtype: np.float32);

        var vector = _services.GetRequiredService<ITextEmbedding>();

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

    public float[] GetTextEmbedding(string text)
    {
        var knowledgeSettings = _services.GetRequiredService<KnowledgeBaseSettings>();
        var embedding = _services.GetServices<ITextEmbedding>()
            .FirstOrDefault(x => x.GetType().FullName.EndsWith(knowledgeSettings.TextEmbedding));

        return embedding.GetVector(text);
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
}
