using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Linq.Dynamic.Core;

namespace BotSharp.Plugin.ExcelHandler.Functions;

public class ReadExcelFn : IFunctionCallback
{
    public string Name => "util-excel-handle_excel_request";
    public string Indication => "Importing data from file...";

    private readonly IServiceProvider _services;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<ReadExcelFn> _logger;
    private readonly BotSharpOptions _options;

    private HashSet<string> _excelFileTypes;

    public ReadExcelFn(
        IServiceProvider services,
        ILogger<ReadExcelFn> logger,
        BotSharpOptions options,
        IFileStorageService fileStorage)
    {
        _services = services;
        _logger = logger;
        _options = options;
        _fileStorage = fileStorage;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs, _options.JsonSerializerOptions);
        var conv = _services.GetRequiredService<IConversationService>();
        var states = _services.GetRequiredService<IConversationStateService>();
        var routingCtx = _services.GetRequiredService<IRoutingContext>();

        Init();

        var dialogs = routingCtx.GetDialogs();
        if (dialogs.IsNullOrEmpty())
        {
            dialogs = await conv.GetDialogHistory();
        }

        var isExcelExist = AssembleFiles(conv.ConversationId, dialogs);
        if (!isExcelExist)
        {
            message.Content = "No excel files found in the conversation";
            return true;
        }

        var results = ImportDataFromDialogs(message, dialogs);
        message.Content = GenerateSqlExecutionSummary(results);
        states.SetState("data_import_result", message.Content);
        dialogs.ForEach(x => x.Files = null);
        return true;
    }


    #region Private Methods
    private void Init()
    {
        if (_excelFileTypes.IsNullOrEmpty())
        {
            _excelFileTypes = FileUtility.GetMimeFileTypes(["excel", "spreadsheet", "csv"]).ToHashSet();
        }
    }

    private bool AssembleFiles(string conversationId, List<RoleDialogModel> dialogs)
    {
        if (dialogs.IsNullOrEmpty())
        {
            return false;
        }

        var messageIds = dialogs.Select(x => x.MessageId).Distinct().ToList();
        var contentTypes = FileUtility.GetContentFileTypes(mimeTypes: _excelFileTypes);
        var excelFiles = _fileStorage.GetMessageFiles(conversationId, messageIds, options: new()
        {
            Sources = [FileSource.User],
            ContentTypes = contentTypes
        });

        if (excelFiles.IsNullOrEmpty())
        {
            return false;
        }

        foreach (var dialog in dialogs)
        {
            var found = excelFiles.Where(x => x.MessageId == dialog.MessageId
                                           && x.FileSource.IsEqualTo(FileSource.User)).ToList();
            
            if (found.IsNullOrEmpty() || !dialog.IsFromUser)
            {
                continue;
            }

            dialog.Files = found.Select(x => new BotSharpFile
            {
                ContentType = x.ContentType,
                FileUrl = x.FileUrl,
                FileStorageUrl = x.FileStorageUrl
            }).ToList();
        }

        return true;
    }

    private List<SqlContextOut> ImportDataFromDialogs(RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var dbHook = _services.GetRequiredService<IText2SqlHook>();
        var dbType = dbHook.GetDatabaseType(message);
        var dbService = _services.GetServices<IDbService>().First(x => x.Provider == dbType);
        var sqlCommands = new List<SqlContextOut>();
        var dialog = dialogs.Last(x => !x.Files.IsNullOrEmpty());
        
        foreach (var file in dialog.Files)
        {
            if (string.IsNullOrWhiteSpace(file?.FileStorageUrl))
            {
                continue;
            }

            string extension = Path.GetExtension(file.FileStorageUrl);
            if (!_excelFileTypes.Contains(extension))
            {
                continue;
            }

            var binary = _fileStorage.GetFileBytes(file.FileStorageUrl);
            var workbook = ConvertToWorkbook(binary, extension);

            var currentCommands = dbService.WriteExcelDataToDB(message, workbook);
            sqlCommands.AddRange(currentCommands);
        }
        return sqlCommands;
    }

    private string GenerateSqlExecutionSummary(List<SqlContextOut> results)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append("\r\n");
        foreach (var result in results)
        {
            stringBuilder.Append(result.Message);
            stringBuilder.Append("\r\n\r\n");
        }

        return stringBuilder.ToString();
    }

    private IWorkbook ConvertToWorkbook(BinaryData binary, string extension)
    {
        var bytes = binary.ToArray();

        if (extension.IsEqualTo(".csv"))
        {
            return ExcelHelper.ConvertCsvToWorkbook(bytes);
        }

        using var fileStream = new MemoryStream(bytes);
        if (extension.IsEqualTo(".xls"))
        {
            return new HSSFWorkbook(fileStream);
        }

        return new XSSFWorkbook(fileStream);
    }
    #endregion
}
