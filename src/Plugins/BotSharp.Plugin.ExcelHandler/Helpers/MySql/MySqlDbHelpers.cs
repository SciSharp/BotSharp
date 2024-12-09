using System.Text.RegularExpressions;
using BotSharp.Plugin.SqlDriver.Settings;
using MySql.Data.MySqlClient;

namespace BotSharp.Plugin.ExcelHandler.Helpers.MySql
{
    public class MySqlDbHelpers : IMySqlDbHelper
    {

        private string _mySqlDriverConnection = "";
        private readonly IServiceProvider _services;
        private string _databaseName;

        public MySqlDbHelpers(IServiceProvider service)
        {
            _services = service;
        }
        public MySqlConnection GetDbConnection()
        {
            if (string.IsNullOrEmpty(_mySqlDriverConnection)) 
            {
                InitializeDatabase();
            }
            var dbConnection = new MySqlConnection(_mySqlDriverConnection);
            dbConnection.Open();
            return dbConnection;
        }

        private void InitializeDatabase()
        {
            var settingService = _services.GetRequiredService<SqlDriverSetting>();
            _mySqlDriverConnection = settingService.MySqlTempConnectionString;
            _databaseName = GetDatabaseName(settingService.MySqlTempConnectionString);
        }

        private string GetDatabaseName(string connectionString)
        {
            string pattern = @"database=([^;]+)";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            Match match = regex.Match(connectionString);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }
}
