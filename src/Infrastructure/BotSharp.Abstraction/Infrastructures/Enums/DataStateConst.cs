namespace BotSharp.Abstraction.Infrastructures.Enums;

/// <summary>
/// Conversation states shared by the data analytics plugins (Planner, SqlDriver, ExcelHandler).
/// </summary>
public static class DataStateConst
{
    public const string DICTIONARY_ITEMS = "dictionary_items";
    public const string TABLE_DDLS = "table_ddls";
    public const string TMP_TABLE = "tmp_table";
    public const string DATA_IMPORT_RESULT = "data_import_result";
}
