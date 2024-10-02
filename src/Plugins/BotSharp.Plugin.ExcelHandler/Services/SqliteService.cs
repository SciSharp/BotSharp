using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Plugin.ExcelHandler.Helpers.Sqlite;
using BotSharp.Plugin.ExcelHandler.Models;
using Microsoft.Data.Sqlite;
using NPOI.SS.UserModel;

namespace BotSharp.Plugin.ExcelHandler.Services
{
    public class SqliteService : ISqliteService
    {
        private readonly ISqliteDbHelpers _sqliteDbHelpers;

        private double _excelRowSize = 0;
        private double _excelColumnSize = 0;
        private string _tableName = "tempTable";
        private string _currentFileName = string.Empty;
        private List<string> _headerColumns = new List<string>();
        private List<string> _columnTypes = new List<string>();

        public SqliteService(ISqliteDbHelpers sqliteDbHelpers)
        {
            _sqliteDbHelpers = sqliteDbHelpers;
        }

        public IEnumerable<SqlContextOut> WriteExcelDataToDB(IWorkbook workbook)
        {
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
        }
        public void DeleteTableSqlQuery()
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
            var physicalDbConnection = _sqliteDbHelpers.GetPhysicalDbConnection();
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

        public string GenerateTableSchema()
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

        #region private methods
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
            string insertSqlQuery = $"Insert into {_tableName} {transferedCols} Values {dataSql}";
            return insertSqlQuery;
        }

        private void ExecuteSqlQueryForInsertion(string query)
        {
            var physicalDbConnection = _sqliteDbHelpers.GetPhysicalDbConnection();
            var inMemoryDbConnection = _sqliteDbHelpers.GetInMemoryDbConnection();

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
        #endregion
    }
}
