using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Linq.Dynamic.Core;

namespace BotSharp.Plugin.ExcelHandler.Functions;

public class ReadExcelFn : IFunctionCallback
{
    public string Name => "util-excel-handle_excel_request";
    public string Indication => "Reading excel";

    private readonly IServiceProvider _services;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<ReadExcelFn> _logger;
    private readonly BotSharpOptions _options;
    private readonly IDbService _dbService;
    private readonly ExcelHandlerSettings _settings;

    private HashSet<string> _excelFileTypes;

    public ReadExcelFn(
        IServiceProvider services,
        ILogger<ReadExcelFn> logger,
        BotSharpOptions options,
        ExcelHandlerSettings settings,
        IFileStorageService fileStorage,
        IEnumerable<IDbService> dbServices)
    {
        _services = services;
        _logger = logger;
        _options = options;
        _settings = settings;
        _fileStorage = fileStorage;
        _dbService = dbServices.FirstOrDefault(x => x.Provider == _settings.Database?.Provider);
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
            dialogs = conv.GetDialogHistory();
        }

        var isExcelExist = AssembleFiles(conv.ConversationId, dialogs);
        if (!isExcelExist)
        {
            message.Content = "No excel files found in the conversation";
            return true;
        }

        var results = GetResponeFromDialogs(dialogs);
        message.Content = GenerateSqlExecutionSummary(results);
        states.SetState("excel_import_result",message.Content);
        dialogs.ForEach(x => x.Files = null);
        return true;
    }


    #region Private Methods
    private void Init()
    {
        if (_excelFileTypes.IsNullOrEmpty())
        {
            _excelFileTypes = FileUtility.GetMimeFileTypes(["excel", "spreadsheet"]).ToHashSet();
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

    private List<SqlContextOut> GetResponeFromDialogs(List<RoleDialogModel> dialogs)
    {
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
            var workbook = ConvertToWorkBook(binary);

            var currentCommands = _dbService.WriteExcelDataToDB(workbook);
            sqlCommands.AddRange(currentCommands);
        }
        return sqlCommands;
    }

    private string GenerateSqlExecutionSummary(List<SqlContextOut> results)
    {
        var stringBuilder = new StringBuilder();
        if (results.Any(x => x.isSuccessful))
        {
            stringBuilder.Append("---Success---");
            stringBuilder.Append("\r\n");
            foreach (var result in results.Where(x => x.isSuccessful))
            {
                stringBuilder.Append(result.Message);
                stringBuilder.Append("\r\n\r\n");
            }
        }
        if (results.Any(x => !x.isSuccessful))
        {
            stringBuilder.Append("---Failed---");
            stringBuilder.Append("\r\n");
            foreach (var result in results.Where(x => !x.isSuccessful))
            {
                stringBuilder.Append(result.Message);
                stringBuilder.Append("\r\n");
            }
        }
        return stringBuilder.ToString();
    }

    private IWorkbook ConvertToWorkBook(BinaryData binary)
    {
        using var fileStream = new MemoryStream(binary.ToArray());
        IWorkbook workbook = new XSSFWorkbook(fileStream);
        return workbook;
    }
    #endregion
}
