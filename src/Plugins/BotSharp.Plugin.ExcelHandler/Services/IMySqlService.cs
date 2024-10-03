using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Plugin.ExcelHandler.Models;
using NPOI.SS.UserModel;

namespace BotSharp.Plugin.ExcelHandler.Services
{
    public interface IMySqlService : IDbService
    {
        public bool DeleteTableSqlQuery();
        public string GenerateTableSchema();
    }
}
