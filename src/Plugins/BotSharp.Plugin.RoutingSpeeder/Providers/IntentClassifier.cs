using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using static Tensorflow.KerasApi;
using Tensorflow.Keras.Engine;
using Tensorflow.NumPy;
using static Tensorflow.Binding;
using Tensorflow.Keras.Callbacks;
using BotSharp.Plugin.RoutingSpeeder.Settings;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Knowledges.Settings;
using BotSharp.Plugin.RoutingSpeeder.Providers.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Tensorflow.Keras;
using BotSharp.Abstraction.Agents;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.RoutingSpeeder.Providers;

public class IntentClassifier
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private KnowledgeBaseSettings _knowledgeBaseSettings;
    Model _model;
    public Model model => _model;
    private bool _isModelReady;
    public bool isModelReady => _isModelReady;
    private ClassifierSetting _settings;
    private bool _inferenceMode = true;
    private string[] _labels;
    public string[] Labels => _labels == null ? GetLabels() : _labels;

    public IntentClassifier(IServiceProvider services,
        ClassifierSetting settings,
        KnowledgeBaseSettings knowledgeBaseSettings,
        ILogger logger)
    {
        _services = services;
        _settings = settings;
        _knowledgeBaseSettings = knowledgeBaseSettings;
        _logger = logger;
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

        var vector = _services.GetServices<ITextEmbedding>()
            .FirstOrDefault(x => x.GetType().FullName.EndsWith(_knowledgeBaseSettings.TextEmbedding));

        var layers = new List<ILayer>
        {
            keras.layers.InputLayer((vector.Dimension), name: "Input"),
            keras.layers.Dense(256, activation:"relu"),
            keras.layers.Dense(256, activation:"relu"),
            keras.layers.Dense(GetLabels().Length, activation: keras.activations.Softmax)
        };
        _model = keras.Sequential(layers);

#if DEBUG
        Console.WriteLine();
        _model.summary();
#endif
    }

    private void Fit(NDArray x, NDArray y, TrainingParams trainingParams)
    {
        _model.compile(optimizer: keras.optimizers.Adam(trainingParams.LearningRate),
            loss: keras.losses.SparseCategoricalCrossentropy(),
            metrics: new[] { "accuracy" });

        var callback_parameters = new CallbackParams
        {
            Model = _model,
            Epochs = trainingParams.Epochs,
            Verbose = 1,
            Steps = 10
        };

        var earlyStop = new EarlyStopping(callback_parameters, "accuracy");

        var callbacks = new List<ICallback>()
        {
            earlyStop
        };

        var weights = LoadWeights();

        _model.fit(x, y,
            batch_size: trainingParams.BatchSize,
            epochs: trainingParams.Epochs,
            callbacks: callbacks,
            shuffle: true);

        _model.save_weights(weights);

        _isModelReady = true;
    }

    public string LoadWeights()
    {
        var agentService = _services.CreateScope()
            .ServiceProvider
            .GetRequiredService<IAgentService>();

        var weightsFile = Path.Combine(agentService.GetDataDir(), _settings.MODEL_DIR, _settings.WEIGHT_FILE_NAME);

        if (File.Exists(weightsFile) && _inferenceMode)
        {
            _model.load_weights(weightsFile);
            _isModelReady = true;
            Console.WriteLine($"Successfully load the weights!");
        }
        else
        {
            var logInfo = _inferenceMode ? "No available weights." : "Will implement model training process and write trained weights into local";
            _isModelReady = false;
            _logger.LogInformation(logInfo);
        }

        return weightsFile;
    }

    public NDArray GetTextEmbedding(string text)
    {
        var knowledgeSettings = _services.GetRequiredService<KnowledgeBaseSettings>();
        var embedding = _services.GetServices<ITextEmbedding>()
            .FirstOrDefault(x => x.GetType().FullName.EndsWith(knowledgeSettings.TextEmbedding));

        var x = np.zeros((1, embedding.Dimension), dtype: np.float32);
        x[0] = embedding.GetVectorAsync(text).GetAwaiter().GetResult();
        return x;
    }

    public (NDArray, NDArray) PrepareLoadData()
    {
        var agentService = _services.CreateScope()
            .ServiceProvider
            .GetRequiredService<IAgentService>();
        string rootDirectory = Path.Combine(
            agentService.GetDataDir(),
            _settings.RAW_DATA_DIR);
        string saveLabelDirectory = Path.Combine(
            agentService.GetDataDir(),
            _settings.MODEL_DIR,
            _settings.LABEL_FILE_NAME);

        if (!Directory.Exists(rootDirectory))
        {
            Directory.CreateDirectory(rootDirectory);
        }

        int numFiles = Directory.GetFiles(rootDirectory).Length;

        if (numFiles == 0)
        {
            throw new Exception($"No dialogue data found in {rootDirectory} folder! Please put dialogue data in this path: {rootDirectory}");
        }

        // Do embedding and store results
        var vector = _services.GetRequiredService<ITextEmbedding>();
        var vectorList = new List<float[]>();
        var labelList = new List<string>();

        foreach (var filePath in GetFiles())
        {
            var texts = File.ReadAllLines(filePath, Encoding.UTF8).ToList();

            vectorList.AddRange(vector.GetVectorsAsync(texts).GetAwaiter().GetResult());
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            labelList.AddRange(Enumerable.Repeat(fileName, texts.Count).ToList());
        }

        // Sort label to keep the same order
        var uniqueLabelList = labelList.Distinct().OrderBy(x => x).ToArray();

        var x = np.zeros((vectorList.Count, vector.Dimension), dtype: np.float32);
        var y = np.zeros((vectorList.Count, 1), dtype: np.float32);

        for (int i = 0; i < vectorList.Count; i++)
        {
            x[i] = vectorList[i];
            y[i] = (float)Array.IndexOf(uniqueLabelList, labelList[i]);
        }

        return (x, y);
    }

    public string[] GetFiles(string prefix = "")
    {
        var agentService = _services.CreateScope()
            .ServiceProvider
            .GetRequiredService<IAgentService>();
        string rootDirectory = Path.Combine(agentService.GetDataDir(), _settings.RAW_DATA_DIR);

        if (string.IsNullOrEmpty(prefix))
        {
            return Directory.GetFiles(rootDirectory)
            .OrderBy(x => Path.GetFileName(x).Split(".")[^2])
            .ToArray();
        }

        return Directory.GetFiles(rootDirectory)
            .Where(x => Path.GetFileNameWithoutExtension(x)
            .StartsWith(prefix))
            .OrderBy(x => x)
            .ToArray();
    }

    public string[] GetLabels()
    {
        var agentService = _services.CreateScope()
                    .ServiceProvider
                    .GetRequiredService<IAgentService>();
        string labelPath = Path.Combine(
                    agentService.GetDataDir(),
                    _settings.MODEL_DIR,
                    _settings.LABEL_FILE_NAME);

        if (_inferenceMode)
        {
            if (_labels == null)
            {
                if (!File.Exists(labelPath))
                {
                    throw new Exception($"Label file doesn't exist. Please training model first or move label.txt to {labelPath}");
                }
                _labels = File.ReadAllLines(labelPath);
            }
        }
        else
        {
            _labels = GetFiles()
                .Select(x => Path.GetFileName(x).Split(".")[^2])
                .OrderBy(x => x)
                .ToArray();
            File.WriteAllLines(labelPath, _labels);
        }
        return _labels;
    }

    public string Predict(NDArray vector, float confidenceScore = 0.9f)
    {
        if (!_isModelReady)
        {
            InitClassifer();
        }

        // Generate and post-process prediction
        var prob = _model.predict(vector).numpy();
        var probLabel = tf.arg_max(prob, -1).numpy().ToArray<long>();
        prob = np.squeeze(prob, axis: 0);
        var labelIndex = probLabel[0];

        if (prob[probLabel[0]] < confidenceScore)
        {
            return string.Empty;
        }

        return _labels[labelIndex];
    }
    public void InitClassifer()
    {
        Reset();
        Build();
        LoadWeights();
    }

    public void Train(TrainingParams trainingParams)
    {
        _inferenceMode = false;
        Reset();
        (var x, var y) = PrepareLoadData();
        Build();
        Fit(x, y, trainingParams);
    }
}
