using NPOI.SS.UserModel;

namespace BotSharp.Plugin.ExcelHandler.Services;

public interface IDbService
{
    string Provider { get;  }

    IEnumerable<SqlContextOut> WriteExcelDataToDB(RoleDialogModel message, IWorkbook workbook);
}
