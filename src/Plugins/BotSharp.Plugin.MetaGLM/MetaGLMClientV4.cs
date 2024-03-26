using BotSharp.Plugin.MetaGLM.Modules;

namespace BotSharp.Plugin.MetaGLM
{
    public class MetaGLMClientV4
    {
        private MetaGLMSettings _settings;

        public Chat Chat { get; private set; }  

        public Images Images { get; private set; } 

        public Embeddings Embeddings { get; private set; } 

        public MetaGLMClientV4(MetaGLMSettings settings)
        {
            this._settings = settings;
            this.Chat = new Chat(this._settings.ApiKey);
            this.Images = new Images(this._settings.ApiKey);
            this.Embeddings = new Embeddings(this._settings.ApiKey);
        }
    }
}
