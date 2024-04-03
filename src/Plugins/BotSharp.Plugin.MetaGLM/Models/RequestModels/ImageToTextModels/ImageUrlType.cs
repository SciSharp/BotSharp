namespace BotSharp.Plugin.MetaGLM.Models.RequestModels.ImageToTextModels
{
    public class ImageUrlType
    {
        public string url { get; set; }

        public ImageUrlType(string url)
        {
            this.url = url;
        }
    }
}