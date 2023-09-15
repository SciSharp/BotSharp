namespace BotSharp.Plugin.KnowledgeBase.MemVecDb;

public class VecRecord
{
    public int Id { get; set; }
    public float[] Vector { get; set; }
    public string Text { get; set; }

    public override string ToString()
    {
        return Text;
    }
}
