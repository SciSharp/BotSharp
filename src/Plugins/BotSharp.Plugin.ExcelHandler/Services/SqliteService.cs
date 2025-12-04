using Microsoft.Data.Sqlite;
using NPOI.SS.UserModel;

namespace BotSharp.Plugin.ExcelHandler.Services;

public class SqliteService : IDbService
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly ExcelHandlerSettings _settings;

    private double _excelRowSize = 0;
    private double _excelColumnSize = 0;
    private string _tableName = "tempTable";
    private List<string> _headerColumns = new List<string>();
    private List<string> _columnTypes = new List<string>();

    public SqliteService(
        IServiceProvider services,
        ILogger<SqliteService> logger,
        ExcelHandlerSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public string Provider => "sqlite";

    public IEnumerable<SqlContextOut> WriteExcelDataToDB(IWorkbook workbook)
    {
        var numTables = workbook.NumberOfSheets;
        var results = new List<SqlContextOut>();

        for (int sheetIdx = 0; sheetIdx < numTables; sheetIdx++)
        {
            ISheet sheet = workbook.GetSheetAt(sheetIdx);

            // create table
            var (isCreateSuccess, message) = SqlCreateTableFn(sheet);

            results.Add(new SqlContextOut
            {
                IsSuccessful = isCreateSuccess,
                Message = message
            });

            // insert data
            var (isInsertSuccess, insertMessage) = SqlInsertDataFn(sheet);

            results.Add(new SqlContextOut
            {
                IsSuccessful = isInsertSuccess,
                Message = insertMessage
            });
        }
        return results;
    }


    #region Private methods
    private (bool, string) SqlInsertDataFn(ISheet sheet)
    {
        try
        {
            string dataSql = ParseSheetData(sheet);
            string insertDataSql = ProcessInsertSqlQuery(dataSql);
            var insertedRowCount = ExecuteSqlQueryForInsertion(insertDataSql);

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

    private (bool, string) SqlCreateTableFn(ISheet sheet)
    {
        try
        {
            _tableName = sheet.SheetName;
            _headerColumns = ParseSheetColumn(sheet);
            string createTableSql = CreateDBTableSqlString(_tableName, _headerColumns, null);
            var rowCount = ExecuteSqlQueryForInsertion(createTableSql);
            // Get table schema using sqlite query
            var schema = GenerateTableSchema();
            return (true, $"Table `{_tableName}` has been successfully created in {Provider}. Table schema:\r\n{schema}");
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

                switch (cell.CellType)
                {
                    case CellType.String:
                        //if (cell.DateCellValue == null || cell.DateCellValue == DateTime.MinValue)
                        //{
                        //    sb.Append($"{cell.DateCellValue}");
                        //    break;
                        //}
                        stringBuilder.Append($"'{cell.StringCellValue.Replace("'", "''")}'");
                        break;
                    case CellType.Numeric:
                        stringBuilder.Append($"{cell.NumericCellValue}");
                        break;
                    case CellType.Blank:
                        stringBuilder.Append($"null");
                        break;
                    default:
                        stringBuilder.Append($"''");
                        break;
                }

                if (colIdx != (_excelColumnSize - 1))
                {
                    stringBuilder.Append(", ");
                }
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
        var headerColumn = headerRow.Cells.Select(x => x.StringCellValue.Replace(" ", "_")).ToList();
        _excelColumnSize = headerColumn.Count;
        return headerColumn;
    }
    private string CreateDBTableSqlString(string tableName, List<string> headerColumns, List<string>? columnTypes = null)
    {
        var createTableSql = $"CREATE TABLE if not exists {tableName} ( Id INTEGER PRIMARY KEY AUTOINCREMENT, ";

        _columnTypes = columnTypes.IsNullOrEmpty() ? headerColumns.Select(x => "TEXT").ToList() : columnTypes;

        headerColumns = headerColumns.Select((x, i) => $"`{x.Replace(" ", "_")}`" + $" {_columnTypes[i]}").ToList();
        createTableSql += string.Join(", ", headerColumns);
        createTableSql += ");";
        return createTableSql;
    }

    private string ProcessInsertSqlQuery(string dataSql)
    {
        var wrapUpCols = _headerColumns.Select(x => $"`{x}`").ToList();
        var transferedCols = '(' + string.Join(',', wrapUpCols) + ')';
        string insertSqlQuery = $"INSERT INTO {_tableName} {transferedCols} VALUES {dataSql}";
        return insertSqlQuery;
    }

    private int ExecuteSqlQueryForInsertion(string query)
    {
        using var conn = GetDbConnection();

        using var command = new SqliteCommand();
        command.CommandText = query;
        command.Connection = conn;

        return command.ExecuteNonQuery();
    }

    private void DeleteTableSqlQuery()
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
        using var conn = GetDbConnection();
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
    private SqliteConnection GetDbConnection()
    {
        var connectionString = _settings.Database.ConnectionString;
        
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
