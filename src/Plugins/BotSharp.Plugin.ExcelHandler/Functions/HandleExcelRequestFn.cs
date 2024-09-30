using BotSharp.Abstraction.Files.Enums;
using BotSharp.Abstraction.Files.Models;
using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Microsoft.Extensions.Primitives;
using BotSharp.Plugin.ExcelHandler.Helpers;
using System.Data.SqlTypes;
using BotSharp.Plugin.ExcelHandler.Models;
using NPOI.SS.Formula.Functions;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace BotSharp.Plugin.ExcelHandler.Functions;

public class HandleExcelRequestFn : IFunctionCallback
{
    public string Name => "handle_excel_request";
    public string Indication => "Handling excel request";

    private readonly IServiceProvider _serviceProvider;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<HandleExcelRequestFn> _logger;
    private readonly BotSharpOptions _options;
    private readonly IDbHelpers _dbHelpers;

    private HashSet<string> _excelMimeTypes;
    private double _excelRowSize = 0;
    private double _excelColumnSize = 0;
    private string _tableName = "tempTable";
    private string _currentFileName = string.Empty;
    private List<string> _headerColumns = new List<string>();
    private List<string> _columnTypes = new List<string>();

    public HandleExcelRequestFn(
        IServiceProvider serviceProvider,
        IFileStorageService fileStorage,
        ILogger<HandleExcelRequestFn> logger,
        BotSharpOptions options,
        IDbHelpers dbHelpers
        )
    {
        _serviceProvider = serviceProvider;
        _fileStorage = fileStorage;
        _logger = logger;
        _options = options;
        _dbHelpers = dbHelpers;
    }


    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs, _options.JsonSerializerOptions);
        var conv = _serviceProvider.GetRequiredService<IConversationService>();

        if (_excelMimeTypes.IsNullOrEmpty())
        {
            _excelMimeTypes = FileUtility.GetMimeFileTypes(new List<string> { "excel", "spreadsheet" }).ToHashSet<string>();
        }

        var dialogs = conv.GetDialogHistory();
        var isExcelExist = AssembleFiles(conv.ConversationId, dialogs);
        if (!isExcelExist)
        {
            message.Content = "No excel files found in the conversation";
            return true;
        }
        if (!DeleteTable())
        {
            message.Content = "Failed to clear existing tables. Please manually delete all existing tables";
        }
        else
        {
            var resultList = GetResponeFromDialogs(dialogs);
            message.Content = GenerateSqlExecutionSummary(resultList);
        }
        message.StopCompletion = true;
        return true;
    }


    #region Private Methods
    private bool AssembleFiles(string convId, List<RoleDialogModel> dialogs)
    {
        if (dialogs.IsNullOrEmpty()) return false;

        var messageId = dialogs.Select(x => x.MessageId).Distinct().ToList();
        var contentType = FileUtility.GetContentFileTypes(mimeTypes: _excelMimeTypes);
        var excelMessageFiles = _fileStorage.GetMessageFiles(convId, messageId, FileSourceType.User, contentType);

        if (excelMessageFiles.IsNullOrEmpty()) return false;

        dialogs.ForEach(dialog => {
            var found = excelMessageFiles.Where(y => y.MessageId == dialog.MessageId).ToList();
            if (found.IsNullOrEmpty()) return;

            dialog.Files = found.Select(x => new BotSharpFile
            {
                ContentType = x.ContentType,
                FileUrl = x.FileUrl,
                FileStorageUrl = x.FileStorageUrl
            }).ToList();
        });
        return true;
    }

    private List<SqlContextOut> GetResponeFromDialogs(List<RoleDialogModel> dialogs)
    {
        var dialog = dialogs.Last(x => !x.Files.IsNullOrEmpty());
        var sqlCommandList = new List<SqlContextOut>();
        foreach (var file in dialog.Files)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.FileStorageUrl)) continue;

            string extension = Path.GetExtension(file.FileStorageUrl);
            if (!_excelMimeTypes.Contains(extension)) continue;
            
            _currentFileName = Path.GetFileName(file.FileStorageUrl);

            var bytes = _fileStorage.GetFileBytes(file.FileStorageUrl);
            var workbook = ConvertToWorkBook(bytes);
            var currentCommandList = WriteExcelDataToDB(workbook);
            sqlCommandList.AddRange(currentCommandList);
        }
        return sqlCommandList;
    }

    private List<SqlContextOut> WriteExcelDataToDB(IWorkbook workbook)
    {
        var numTables = workbook.NumberOfSheets;
        var commandList = new List<SqlContextOut>();

        for (int sheetIdx = 0; sheetIdx < numTables; sheetIdx++)
        {
            var commandResult = new SqlContextOut();
            ISheet sheet = workbook.GetSheetAt(sheetIdx);
            var (isCreateSuccess, message) = SqlCreateTableFn(sheet);

            if (!isCreateSuccess)
            {
                commandResult = new SqlContextOut
                {
                    isSuccessful = isCreateSuccess,
                    Message = message,
                    FileName = _currentFileName
                };
                commandList.Add(commandResult);
                continue;
            }
            var (isInsertSuccess, insertMessage) = SqlInsertDataFn(sheet);
            commandResult = new SqlContextOut
            {
                isSuccessful = isInsertSuccess,
                Message = insertMessage,
                FileName = _currentFileName
            };
            commandList.Add(commandResult);
        }
        return commandList;
    }

    private bool DeleteTable()
    {
        try
        {
            DeleteTableSqlQuery();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete table");
            return false;
        }
    }

    private (bool, string) SqlInsertDataFn(ISheet sheet)
    {
        try
        {
            string dataSql = ParseSheetData(sheet);
            string insertDataSql = ProcessInsertSqlQuery(dataSql);
            ExecuteSqlQueryForInsertion(insertDataSql);

            return (true, $"{_currentFileName}: \r\n `**{_excelRowSize}**` data have been successfully stored into `{_tableName}` table");
        }
        catch (Exception ex)
        {
            return (false, $"{_currentFileName}: Failed to parse excel data into `{_tableName}` table. ####Error: {ex.Message}");
        }
    }

    private string GenerateSqlExecutionSummary(List<SqlContextOut> messageList)
    {
        var stringBuilder = new StringBuilder();
        if (messageList.Any(x => x.isSuccessful))
        {
            stringBuilder.Append("---Success---");
            stringBuilder.Append("\r\n");
            foreach (var message in messageList.Where(x => x.isSuccessful))
            {
                stringBuilder.Append(message.Message);
                string tableSchemaInfo = GenerateTableSchema();
                stringBuilder.Append(tableSchemaInfo);
                stringBuilder.Append("\r\n\r\n");
            }
        }
        if (messageList.Any(x => !x.isSuccessful))
        {
            stringBuilder.Append("---Failed---");
            stringBuilder.Append("\r\n");
            foreach (var message in messageList.Where(x => !x.isSuccessful))
            {
                stringBuilder.Append(message.Message);
                stringBuilder.Append("\r\n");
            }
        }
        return stringBuilder.ToString();
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

    private IWorkbook ConvertToWorkBook(byte[] bytes)
    {
        IWorkbook workbook;
        using (var fileStream = new MemoryStream(bytes))
        {
            workbook = new XSSFWorkbook(fileStream);
        }
        return workbook;
    }

    private (bool, string) SqlCreateTableFn(ISheet sheet)
    {
        try
        {
            _tableName = sheet.SheetName;
            _headerColumns = ParseSheetColumn(sheet);
            string createTableSql = CreateDBTableSqlString(_tableName, _headerColumns, null);
            ExecuteSqlQueryForInsertion(createTableSql);
            return (true, $"{_tableName} has been successfully created.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
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


    private void ExecuteSqlQueryForInsertion(string query)
    {
        var physicalDbConnection = _dbHelpers.GetPhysicalDbConnection();
        var inMemoryDbConnection = _dbHelpers.GetInMemoryDbConnection();

        physicalDbConnection.BackupDatabase(inMemoryDbConnection, "main", "main");
        physicalDbConnection.Close();

        using (var command = new SqliteCommand())
        {
            command.CommandText = query;
            command.Connection = inMemoryDbConnection;
            command.ExecuteNonQuery();
        }
        inMemoryDbConnection.BackupDatabase(physicalDbConnection);
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
        var physicalDbConnection = _dbHelpers.GetPhysicalDbConnection();
        using var selectCmd = new SqliteCommand(deleteTableSql, physicalDbConnection);
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
                using var dropTableCommand = new SqliteCommand(query, physicalDbConnection);
                dropTableCommand.ExecuteNonQuery();
            });
        }
        physicalDbConnection.Close();
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
                        stringBuilder.Append($"'{cell.StringCellValue}'");
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

    private string ProcessInsertSqlQuery(string dataSql)
    {
        var wrapUpCols = _headerColumns.Select(x => $"`{x}`").ToList();
        var transferedCols = '('+ string.Join(',', wrapUpCols) + ')';
        string insertSqlQuery = $"Insert into {_tableName} {transferedCols} Values {dataSql}";
        return insertSqlQuery;
    }


    [Obsolete("This method is not used anymore", true)]
    private (bool, string) ParseExcelDataToSqlString(ISheet sheet)
    {
        try
        {
            if (_headerColumns.IsNullOrEmpty())
            {
                _headerColumns = ParseSheetColumn(sheet);
                string createTableSql = CreateDBTableSqlString(_tableName, _headerColumns, null);
                ExecuteSqlQueryForInsertion(createTableSql);
            }

            string dataSql = ParseSheetData(sheet);
            string insertDataSql = ProcessInsertSqlQuery(dataSql);
            ExecuteSqlQueryForInsertion(insertDataSql);
            return (true, $"{_currentFileName}: {_excelRowSize} data have been successfully stored into {_tableName}");
        }
        catch (Exception ex)
        {
            return (false, $"{_currentFileName}: Failed to parse excel data to sql string. Error: {ex.Message}");
        }
    }

    [Obsolete("This method is not used anymore", true)]
    private bool IsHeaderColumnEqual(List<string> headerColumn)
    {
        if (_headerColumns.IsNullOrEmpty() || _headerColumns.Count != headerColumn.Count)
        {
            return false;
        }

        return new HashSet<string>(headerColumn).SetEquals(_headerColumns);
    }
    #endregion
}
