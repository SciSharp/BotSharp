using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Plugin.ExcelHandler.Models;
using NPOI.SS.UserModel;

namespace BotSharp.Plugin.ExcelHandler.Services
{
    public interface IDbService
    {
        IEnumerable<SqlContextOut> WriteExcelDataToDB(IWorkbook workbook);
    }
}
