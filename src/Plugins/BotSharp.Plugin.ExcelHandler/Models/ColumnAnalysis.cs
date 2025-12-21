namespace BotSharp.Plugin.ExcelHandler.Models;

public class ColumnAnalysis
{
    public string ColumnName { get; set; } = string.Empty;
    public HashSet<string> DistinctValues { get; set; } = new();
    public int TotalCount { get; set; }
    public int NullCount { get; set; }
    public int NumericCount { get; set; }
    public int DateCount { get; set; }
    public int BooleanCount { get; set; }
    public int IntegerCount { get; set; }
}

