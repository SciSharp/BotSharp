using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;
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
        TrainingCorpus ExtractorCorpus(TAgent agent);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TStorage"></typeparam>
        /// <param name="agent"></param>
        /// <returns></returns>
        bool SaveAgent(TAgent agent);

        Task<bool> Train(TAgent agent, TrainingCorpus corpus);
    }
}
