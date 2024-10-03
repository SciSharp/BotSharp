using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;

namespace BotSharp.Plugin.ExcelHandler.Helpers.MySql
{
    public interface IMySqlDbHelper
    {
        MySqlConnection GetDbConnection();
    }
}
