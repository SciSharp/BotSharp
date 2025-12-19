using Microsoft.Data.Sqlite;
using NPOI.SS.UserModel;

namespace BotSharp.Plugin.ExcelHandler.Services;

public class SqliteService : IDbService
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    private double _excelRowSize = 0;
    private double _excelColumnSize = 0;
    private string _tableName = "tempTable";
    private List<string> _headerColumns = new List<string>();
    private List<string> _columnTypes = new List<string>();

    public SqliteService(
        IServiceProvider services,
        ILogger<SqliteService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Provider => "sqlite";

    public IEnumerable<SqlContextOut> WriteExcelDataToDB(RoleDialogModel message, IWorkbook workbook)
    {
        var numTables = workbook.NumberOfSheets;
        var results = new List<SqlContextOut>();

        for (int sheetIdx = 0; sheetIdx < numTables; sheetIdx++)
        {
            ISheet sheet = workbook.GetSheetAt(sheetIdx);

            // clear existing data
            DeleteTableSqlQuery(message);

            // create table
            var (isCreateSuccess, msg) = SqlCreateTableFn(message, sheet);

            results.Add(new SqlContextOut
            {
                IsSuccessful = isCreateSuccess,
                Message = msg
            });

            // insert data
            var (isInsertSuccess, insertMessage) = SqlInsertDataFn(message, sheet);

            results.Add(new SqlContextOut
            {
                IsSuccessful = isInsertSuccess,
                Message = insertMessage
            });
        }
        return results;
    }


    #region Private methods
    private (bool, string) SqlInsertDataFn(RoleDialogModel message, ISheet sheet)
    {
        try
        {
            string dataSql = ParseSheetData(sheet);
            string insertDataSql = ProcessInsertSqlQuery(dataSql);
            var insertedRowCount = ExecuteSqlQueryForInsertion(message, insertDataSql);

            // List top 3 rows
            var top3rows = dataSql.Split("\r").Take(3);
            var dataSample = string.Join("\r", dataSql.Split("\r").Take(3)).Trim(',', ' ');

            return (true, $"{insertedRowCount} records have been successfully inserted into `{_tableName}` table. Top {top3rows.Count()} rows:\r\n{dataSample}");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to parse excel data into `{_tableName}` table. ####Error: {ex.Message}");
        }
    }

    private (bool, string) SqlCreateTableFn(RoleDialogModel message, ISheet sheet)
    {
        try
        {
            _tableName = sheet.SheetName;
            _headerColumns = ParseSheetColumn(sheet);

            // Collect column distinct values for type inference
            var columnAnalyses = CollectColumnDistinctValues(sheet);
            var inferredTypes = InferColumnTypes(columnAnalyses);

            // generate column summary
            var analysisSummary = GenerateColumnAnalysisSummary(columnAnalyses);
            _logger.LogInformation("Column Analysis:\n{Summary}", analysisSummary);

            string createTableSql = CreateDBTableSqlString(_tableName, _headerColumns, inferredTypes);
            var rowCount = ExecuteSqlQueryForInsertion(message, createTableSql);

            // Get table schema using sqlite query
            var schema = GenerateTableSchema();
            return (true, $"Table `{_tableName}` has been successfully created in {Provider}. Table schema:\r\n{schema}\r\n\r\nColumn Analysis:\r\n{analysisSummary}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private string ParseSheetData(ISheet singleSheet)
    {
        var stringBuilder = new StringBuilder();

        for (int rowIdx = 1; rowIdx < _excelRowSize + 1; rowIdx++)
        {
            IRow row = singleSheet.GetRow(rowIdx);
            stringBuilder.Append('(');
            for (int colIdx = 0; colIdx < _excelColumnSize; colIdx++)
            {
                var cell = row.GetCell(colIdx, MissingCellPolicy.CREATE_NULL_AS_BLANK);
                stringBuilder.Append(FormatCellForSql(cell));
                if (colIdx != (_excelColumnSize - 1))
                    stringBuilder.Append(", ");
            }
            stringBuilder.Append(')');
            stringBuilder.Append(rowIdx == _excelRowSize ? ';' : ", \r\n");
        }
        return stringBuilder.ToString();
    }

    private List<string> ParseSheetColumn(ISheet sheet)
    {
        if (sheet.PhysicalNumberOfRows < 2)
            throw new Exception("No data found in the excel file");

        _excelRowSize = sheet.PhysicalNumberOfRows - 1;
        var headerRow = sheet.GetRow(0);
        var headerColumn = headerRow.Cells.Select(x => x.StringCellValue.Replace(" ", "_").Replace("#", "_")).ToList();
        _excelColumnSize = headerColumn.Count;
        return headerColumn;
    }

    private Dictionary<string, ColumnAnalysis> CollectColumnDistinctValues(ISheet sheet, int maxDistinctCount = 100)
    {
        var result = new Dictionary<string, ColumnAnalysis>();

        for (int colIdx = 0; colIdx < _excelColumnSize; colIdx++)
        {
            var columnName = _headerColumns[colIdx];
            var analysis = new ColumnAnalysis
            {
                ColumnName = columnName,
                DistinctValues = new HashSet<string>(),
                TotalCount = 0,
                NullCount = 0,
                NumericCount = 0,
                DateCount = 0,
                BooleanCount = 0,
                IntegerCount = 0
            };

            for (int rowIdx = 1; rowIdx <= _excelRowSize; rowIdx++)
            {
                var row = sheet.GetRow(rowIdx);
                if (row == null)
                {
                    analysis.NullCount++;
                    analysis.TotalCount++;
                    continue;
                }

                var cell = row.GetCell(colIdx, MissingCellPolicy.CREATE_NULL_AS_BLANK);
                analysis.TotalCount++;

                var (cellValue, cellType) = GetCellValueAndType(cell);

                switch (cellType)
                {
                    case "NULL":
                        analysis.NullCount++;
                        break;
                    case "NUMERIC":
                        analysis.NumericCount++;
                        if (IsInteger(cell)) analysis.IntegerCount++;
                        break;
                    case "DATE":
                        analysis.DateCount++;
                        break;
                    case "BOOLEAN":
                        analysis.BooleanCount++;
                        break;
                }

                if (analysis.DistinctValues.Count < maxDistinctCount && !string.IsNullOrEmpty(cellValue))
                {
                    analysis.DistinctValues.Add(cellValue);
                }
            }

            result[columnName] = analysis;
        }

        return result;
    }

    private string FormatCellForSql(ICell cell)
    {
        var (value, type) = GetCellValueAndType(cell);
        return type == "NULL" ? "null"
             : type == "NUMERIC" ? value
             : type == "BOOLEAN" ? (value.ToLower() is "true" or "yes" or "1" ? "1" : "0")
             : $"'{value.Replace("'", "''")}'";
    }

    private (string value, string type) GetCellValueAndType(ICell cell)
        => cell.CellType switch
        {
            CellType.String => IsBooleanString(cell.StringCellValue?.Trim())
                ? (cell.StringCellValue.Trim(), "BOOLEAN")
                : (cell.StringCellValue?.Trim() ?? "", "TEXT"),
            CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                ? (cell.DateCellValue?.ToString("yyyy-MM-dd HH:mm:ss") ?? "", "DATE")
                : (cell.NumericCellValue.ToString(), "NUMERIC"),
            CellType.Boolean => (cell.BooleanCellValue.ToString(), "BOOLEAN"),
            CellType.Blank => ("", "NULL"),
            _ => (cell.ToString() ?? "", "TEXT")
        };

    private bool IsInteger(ICell cell)
    {
        if (cell.CellType != CellType.Numeric) return false;
        var value = cell.NumericCellValue;
        return Math.Abs(value - Math.Floor(value)) < 0.0001;
    }

    private bool IsBooleanString(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        var lower = value.ToLower();
        return lower == "true" || lower == "false" ||
               lower == "yes" || lower == "no" ||
               lower == "1" || lower == "0";
    }

    private List<string> InferColumnTypes(Dictionary<string, ColumnAnalysis> columnAnalyses)
        => _headerColumns.Select(col =>
        {
            var a = columnAnalyses[col];
            var n = a.TotalCount - a.NullCount;
            return n == 0 ? "TEXT"
                 : a.DateCount == n ? "DATE"
                 : a.BooleanCount == n ? "BOOLEAN"
                 : a.NumericCount == n ? (a.IntegerCount == a.NumericCount ? "INTEGER" : "REAL")
                 : "TEXT";
        }).ToList();

    public string GenerateColumnAnalysisSummary(Dictionary<string, ColumnAnalysis> columnAnalyses)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Column Analysis Summary:");
        sb.AppendLine("========================");

        foreach (var kvp in columnAnalyses)
        {
            var col = kvp.Value;
            sb.AppendLine($"\n[{col.ColumnName}]");
            sb.AppendLine($"  Total: {col.TotalCount}, Null: {col.NullCount}");
            sb.AppendLine($"  Numeric: {col.NumericCount}, Integer: {col.IntegerCount}, Date: {col.DateCount}, Boolean: {col.BooleanCount}");
            sb.AppendLine($"  Distinct Values ({col.DistinctValues.Count}): {string.Join(", ", col.DistinctValues.Take(10))}{(col.DistinctValues.Count > 10 ? "..." : "")}");
        }

        return sb.ToString();
    }

    private string CreateDBTableSqlString(string tableName, List<string> headerColumns, List<string>? columnTypes = null)
    {
        _columnTypes = columnTypes.IsNullOrEmpty() ? headerColumns.Select(x => "TEXT").ToList() : columnTypes;

        var sanitizedColumns = headerColumns.Select(x => x.Replace(" ", "_").Replace("#", "_")).ToList();
        var columnDefs = sanitizedColumns.Select((col, i) => $"`{col}` {_columnTypes[i]}");
        var createTableSql = $"CREATE TABLE IF NOT EXISTS {tableName} ( Id INTEGER PRIMARY KEY AUTOINCREMENT, {string.Join(", ", columnDefs)} );";

        // Create index for each column
        var indexSql = sanitizedColumns.Select(col => $"CREATE INDEX IF NOT EXISTS idx_{tableName}_{col} ON {tableName} (`{col}`);");

        return createTableSql + "\n" + string.Join("\n", indexSql);
    }

    private string ProcessInsertSqlQuery(string dataSql)
    {
        var wrapUpCols = _headerColumns.Select(x => $"`{x}`").ToList();
        var transferedCols = '(' + string.Join(',', wrapUpCols) + ')';
        string insertSqlQuery = $"INSERT INTO {_tableName} {transferedCols} VALUES {dataSql}";
        return insertSqlQuery;
    }

    private int ExecuteSqlQueryForInsertion(RoleDialogModel message, string query)
    {
        using var conn = GetDbConnection(message);

        using var command = new SqliteCommand();
        command.CommandText = query;
        command.Connection = conn;

        return command.ExecuteNonQuery();
    }

    private void DeleteTableSqlQuery(RoleDialogModel message)
    {
        string deleteTableSql = @"
                SELECT
                    name
                FROM
                    sqlite_schema
                WHERE
                    type = 'table' AND
                    name NOT LIKE 'sqlite_%'
            ";
        using var conn = GetDbConnection(message);
        using var selectCmd = new SqliteCommand(deleteTableSql, conn);
        using var reader = selectCmd.ExecuteReader();
        if (reader.HasRows)
        {
            var dropTableQueries = new List<string>();
            while (reader.Read())
            {
                string tableName = reader.GetString(0);
                var dropTableSql = $"DROP TABLE IF EXISTS '{tableName}'";
                dropTableQueries.Add(dropTableSql);
            }
            dropTableQueries.ForEach(query =>
            {
                using var dropTableCommand = new SqliteCommand(query, conn);
                dropTableCommand.ExecuteNonQuery();
            });
        }
        conn.Close();
    }

    private string GenerateTableSchema()
    {
        var sb = new StringBuilder();
        sb.Append($"\nTable Schema for `{_tableName}`:");
        sb.Append("\n");
        sb.Append($"cid | name       | type      ");
        sb.Append("\n");
        //sb.Append("----|------------|------------");
        for (int i = 0; i < _excelColumnSize; i++)
        {
            sb.Append($"{i,-4}   | {_headerColumns[i],-10} | {_columnTypes[i],-10}");
            sb.Append("\n");
        }
        return sb.ToString();
    }
    #endregion

    #region Db connection
    private SqliteConnection GetDbConnection(RoleDialogModel message)
    {
        var sqlHook = _services.GetRequiredService<IText2SqlHook>();
        var connectionString = sqlHook.GetConnectionString(message);

        // Extract the database file path from the connection string
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dbFilePath = builder.DataSource;

        _logger.LogInformation("Database file path: {DbFilePath}", dbFilePath);
        
        // If it's not an in-memory database, ensure the directory exists
        if (!string.IsNullOrEmpty(dbFilePath) && 
            !dbFilePath.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            var directory = Path.GetDirectoryName(dbFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created directory: {Directory}", directory);
            }
        }
        
        // SQLite automatically creates the database file when opening the connection
        var dbConnection = new SqliteConnection(connectionString);
        dbConnection.Open();
        return dbConnection;
    }
    #endregion
}
