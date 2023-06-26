namespace BotSharp.Core.Plugins.MemVecDb;

public class VecRecord
{
    public int Id { get; set; }
    public float[] Vector { get; set; }
    public string Text { get; set; }
}
