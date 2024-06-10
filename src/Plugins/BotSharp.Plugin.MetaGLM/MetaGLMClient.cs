using BotSharp.Plugin.MetaGLM.Modules;

namespace BotSharp.Plugin.MetaGLM
{
    public class MetaGLMClient
    {
        private MetaGLMSettings _settings;

        public Chat Chat { get; private set; }  

        public Images Images { get; private set; } 

        public Embeddings Embeddings { get; private set; } 

        public MetaGLMClient(MetaGLMSettings settings)
        {
            this._settings = settings;
            this.Chat = new Chat(this._settings.ApiKey, _settings.BaseAddress);
            this.Images = new Images(this._settings.ApiKey, _settings.BaseAddress);
            this.Embeddings = new Embeddings(this._settings.ApiKey, _settings.BaseAddress);
        }
    }
}
