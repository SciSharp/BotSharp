using NPOI.SS.UserModel;

namespace BotSharp.Plugin.ExcelHandler.Services;

public interface IDbService
{
    string Provider { get;  }

    IEnumerable<SqlContextOut> WriteExcelDataToDB(IWorkbook workbook);
}
