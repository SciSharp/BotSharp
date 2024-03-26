namespace BotSharp.Plugin.MetaGLM.Models.ResponseModels.EmbeddingModels;

public class EmbeddingDataItem
{
    public int index { get; set; }
    public string _object { get; set; }
    public double[] embedding { get; set; }
}