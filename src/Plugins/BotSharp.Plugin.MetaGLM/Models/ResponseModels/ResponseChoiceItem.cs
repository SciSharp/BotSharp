namespace BotSharp.Plugin.MetaGLM.Models.ResponseModels;

public class ResponseChoiceItem
{
    public string finish_reason { get; set; }
    public int index { get; set; }
    public ResponseChoiceDelta message { get; set; }
    public ResponseChoiceDelta delta { get; set; }
}