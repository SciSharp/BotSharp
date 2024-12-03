namespace BotSharp.Abstraction.Utilities;

public static class MathExt
{
    public static int Max(int a, int b, int c)
    {
        return Math.Max(Math.Max(a, b), c);
    }

    public static long Max(long a, long b, long c)
    {
        return Math.Max(Math.Max(a, b), c);
    }
}
