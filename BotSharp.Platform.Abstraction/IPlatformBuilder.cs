using BotSharp.Platform.Models;
using BotSharp.Platform.Models.AiRequest;
using BotSharp.Platform.Models.AiResponse;
using BotSharp.Platform.Models.MachineLearning;
using System.Threading.Tasks;

namespace BotSharp.Platform.Abstraction
{
    /// <summary>
    /// Platform abstraction
    /// Implement this interface to build a Chatbot platform
    /// </summary>
    public interface IPlatformBuilder<TAgent>
    {
        /// <summary>
        /// Agent storage
        /// </summary>
        IAgentStorage<TAgent> Storage { get; set; }

        /// <summary>
        /// Parse options for the incoming text or voice request from the sender.
        /// </summary>
        // DialogRequestOptions RequestOptions { get; set; }

        /// <summary>
        /// Convert platform specific data to standard training corpus format
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        Task<TrainingCorpus> ExtractorCorpus(TAgent agent);

        /// <summary>
        /// Load agent from files.
        /// There must contain a meta.json
        /// </summary>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        Task<TAgent> LoadAgentFromFile<TImporter>(string dataDir) where TImporter : IAgentImporter<TAgent>, new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TStorage"></typeparam>
        /// <param name="agent"></param>
        /// <returns></returns>
        Task<bool> SaveAgent(TAgent agent);

        Task<ModelMetaData> Train(TAgent agent, TrainingCorpus corpus, BotTrainOptions options);

        Task<TResult> TextRequest<TResult>(AiRequest request);
    }
}
