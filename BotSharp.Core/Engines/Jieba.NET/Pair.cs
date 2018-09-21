namespace JiebaNet.Segmenter
{
    public class Pair<TKey>
    {
        public TKey Key { get;set; }
        public double Freq { get; set; }

        public Pair(TKey key, double freq)
        {
            Key = key;
            Freq = freq;
        }

        public override string ToString()
        {
            return "Candidate [Key=" + Key + ", Freq=" + Freq + "]";
        }
    }
}
