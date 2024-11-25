using System.Data;
using BotSharp.Abstraction.Routing;
using BotSharp.Plugin.ExcelHandler.Helpers.MySql;
using BotSharp.Plugin.ExcelHandler.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using NPOI.SS.UserModel;

namespace BotSharp.Plugin.ExcelHandler.Services
{
    public class MySqlService : IMySqlService
    {
        private readonly IMySqlDbHelper _mySqlDbHelpers;
        private readonly IServiceProvider _services;
        private double _excelRowSize = 0;
        private double _excelColumnSize = 0;
        private string _tableName = "tempTable";
        private string _database = "";
        private string _currentFileName = string.Empty;
        private List<string> _headerColumns = new List<string>();
        private List<string> _columnTypes = new List<string>();

        public MySqlService(IMySqlDbHelper mySqlDbHelpers, IServiceProvider services)
        {
            _mySqlDbHelpers = mySqlDbHelpers;
            _services = services;
        }

        public bool DeleteTableSqlQuery()
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /*private void ExecuteDropTableQuery(List<string> dropTableNames, MySqlConnection connection)
        {
            dropTableNames.ForEach(x =>
            {
                var dropTableQuery = $"DROP TABLE IF EXISTS {x}";

                using var selectCmd = new MySqlCommand(dropTableQuery, connection);
                selectCmd.ExecuteNonQuery();
            });
        }*/

        public List<string> GetAllTableSchema(MySqlConnection mySqlDbConnection)
        {
            string schemaQuery = $@"
            SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_KEY, EXTRA
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = '{mySqlDbConnection.Database}';";

            var tables = new List<string>();

            using MySqlCommand cmd = new MySqlCommand(schemaQuery, mySqlDbConnection);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        string tableName = reader.GetString("TABLE_NAME");
                        //string columnName = reader.GetString("COLUMN_NAME");
                        //string dataType = reader.GetString("DATA_TYPE");
                        //string isNullable = reader.GetString("IS_NULLABLE");
                        //string columnKey = reader.GetString("COLUMN_KEY");
                        //string extra = reader.GetString("EXTRA");
                        tables.Add(tableName);
                    }
                }
                return tables.Distinct().ToList();
            }
        }

        public IEnumerable<SqlContextOut> WriteExcelDataToDB(IWorkbook workbook)
        {
            var numTables = workbook.NumberOfSheets;
            var commandList = new List<SqlContextOut>();
            var state = _services.GetRequiredService<IConversationStateService>();

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

                string table = $"{_database}.{_tableName}";
                state.SetState("tmp_table", table);

                var (isInsertSuccess, insertMessage) = SqlInsertDataFn(sheet);
                string exampleData = GetInsertExample(table);

                commandResult = new SqlContextOut
                {
                    isSuccessful = isInsertSuccess,
                    Message = $"{insertMessage}\r\nExample Data: {exampleData}. \r\n The remaining data contains different values. ",
                    FileName = _currentFileName
                };
                commandList.Add(commandResult);
            }
            return commandList;
        }
        private string ProcessInsertSqlQuery(string dataSql)
        {
            var wrapUpCols = _headerColumns.Select(x => $"`{x}`").ToList();
            var transferedCols = '(' + string.Join(',', wrapUpCols) + ')';
            string insertSqlQuery = $"Insert into {_tableName} {transferedCols} Values {dataSql}";
            return insertSqlQuery;
        }
        private (bool, string) SqlInsertDataFn(ISheet sheet)
        {
            try
            {
                string dataSql = ParseSheetData(sheet);
                string insertDataSql = ProcessInsertSqlQuery(dataSql);
                ExecuteSqlQueryForInsertion(insertDataSql);

                return (true, $"{_currentFileName}: \r\n {_excelRowSize} records have been successfully inserted into `{_database}`.`{_tableName}` table");
            }
            catch (Exception ex)
            {
                return (false, $"{_currentFileName}: Failed to parse excel data into `{_database}`.`{_tableName}` table. ####Error: {ex.Message}");
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

        private (bool, string) SqlCreateTableFn(ISheet sheet)
        {
            try
            {
                var routing = _services.GetRequiredService<IRoutingContext>();
                _tableName = $"excel_{routing.ConversationId.Split('-').Last()}_{sheet.SheetName}";
                _headerColumns = ParseSheetColumn(sheet);
                string createTableSql = CreateDBTableSqlString(_tableName, _headerColumns, null ,true);
                ExecuteSqlQueryForInsertion(createTableSql);
                createTableSql = createTableSql.Replace(_tableName, $"{_database}.{_tableName}");
                return (true, createTableSql);
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
        private string CreateDBTableSqlString(string tableName, List<string> headerColumns, List<string>? columnTypes = null, bool isMemory = false)
        {
            _columnTypes = columnTypes.IsNullOrEmpty() ? headerColumns.Select(x => "VARCHAR(128)").ToList() : columnTypes;

            /*if (!headerColumns.Any(x => x.Equals("id", StringComparison.OrdinalIgnoreCase)))
            {
                headerColumns.Insert(0, "Id");
                _columnTypes?.Insert(0, "INT UNSIGNED AUTO_INCREMENT");
            }*/

            var createTableSql = $"DROP TABLE IF EXISTS {tableName}; CREATE TABLE if not exists {tableName} ( \n";
            createTableSql += string.Join(", \n", headerColumns.Select((x, i) => $"`{x}` {_columnTypes[i]}"));
            var indexSql = string.Join(", \n", headerColumns.Select(x => $"KEY `idx_{tableName}_{x}` (`{x}`)"));
            createTableSql += $", \n{indexSql}\n);";

            return createTableSql;
        }

        public void ExecuteSqlQueryForInsertion(string sqlQuery)
        {
            using var connection = _mySqlDbHelpers.GetDbConnection();
            _database = connection.Database;
            using (MySqlCommand cmd = new MySqlCommand(sqlQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
        private string GetInsertExample(string tableName)
        {
            using var connection = _mySqlDbHelpers.GetDbConnection();
            _database = connection.Database;
            var sqlQuery = $"SELECT * FROM {tableName} LIMIT 2;";
            using var cmd = new MySqlCommand(sqlQuery, connection);
            using var reader = cmd.ExecuteReader();

            var dataExample = new DataTable();
            dataExample.Load(reader);

            return JsonConvert.SerializeObject(dataExample);
        }
    }
}
