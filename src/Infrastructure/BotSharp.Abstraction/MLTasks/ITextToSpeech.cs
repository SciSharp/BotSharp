namespace BotSharp.Abstraction.MLTasks
{
    public interface ITextToSpeech
    {
        /// <summary>
        /// The LLM provider like Microsoft Azure, OpenAI, ClaudAI
        /// </summary>
        string Provider { get; }

        /// <summary>
        /// Set model name, one provider can consume different model or version(s)
        /// </summary>
        /// <param name="model">deployment name</param>
        void SetModelName(string model);

        Task<BinaryData> GenerateSpeechFromTextAsync(string text, ITextToSpeechOptions? options = null);
    }

    public interface ITextToSpeechOptions
    {

    }
}
