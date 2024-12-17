using System.Linq.Dynamic.Core;
using BotSharp.Abstraction.Files.Enums;
using BotSharp.Abstraction.Files.Models;
using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Plugin.ExcelHandler.Models;
using BotSharp.Plugin.ExcelHandler.Services;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace BotSharp.Plugin.ExcelHandler.Functions;

public class HandleExcelRequestFn : IFunctionCallback
{
    public string Name => "util-excel-handle_excel_request";
    public string Indication => "Handling excel request";

    private readonly IServiceProvider _serviceProvider;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<HandleExcelRequestFn> _logger;
    private readonly BotSharpOptions _options;
    private readonly IMySqlService _mySqlService;


    private HashSet<string> _excelMimeTypes;
    private double _excelRowSize = 0;
    private double _excelColumnSize = 0;
    private string _tableName = "tempTable";
    private string _currentFileName = string.Empty;
    private List<string> _headerColumns = new List<string>();
    private List<string> _columnTypes = new List<string>();

    public HandleExcelRequestFn(
        IServiceProvider serviceProvider,
        IFileStorageService fileStorage,
        ILogger<HandleExcelRequestFn> logger,
        BotSharpOptions options,
        IMySqlService mySqlService
        )
    {
        _serviceProvider = serviceProvider;
        _fileStorage = fileStorage;
        _logger = logger;
        _options = options;
        _mySqlService = mySqlService;
    }


    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs, _options.JsonSerializerOptions);
        var conv = _serviceProvider.GetRequiredService<IConversationService>();


        if (_excelMimeTypes.IsNullOrEmpty())
        {
            _excelMimeTypes = FileUtility.GetMimeFileTypes(new List<string> { "excel", "spreadsheet" }).ToHashSet<string>();
        }

        var dialogs = conv.GetDialogHistory();
        var isExcelExist = AssembleFiles(conv.ConversationId, dialogs);
        if (!isExcelExist)
        {
            message.Content = "No excel files found in the conversation";
            return true;
        }

        var resultList = GetResponeFromDialogs(dialogs);
        var states = _serviceProvider.GetRequiredService<IConversationStateService>();

        message.Content = GenerateSqlExecutionSummary(resultList);
        states.SetState("excel_import_result",message.Content);
        
        return true;
    }


    #region Private Methods
    private bool AssembleFiles(string convId, List<RoleDialogModel> dialogs)
    {
        if (dialogs.IsNullOrEmpty()) return false;

        var messageId = dialogs.Select(x => x.MessageId).Distinct().ToList();
        var contentType = FileUtility.GetContentFileTypes(mimeTypes: _excelMimeTypes);
        var excelMessageFiles = _fileStorage.GetMessageFiles(convId, messageId, FileSourceType.User, contentType);

        if (excelMessageFiles.IsNullOrEmpty()) return false;

        dialogs.ForEach(dialog =>
        {
            var found = excelMessageFiles.Where(y => y.MessageId == dialog.MessageId).ToList();
            if (found.IsNullOrEmpty()) return;

            dialog.Files = found.Select(x => new BotSharpFile
            {
                ContentType = x.ContentType,
                FileUrl = x.FileUrl,
                FileStorageUrl = x.FileStorageUrl
            }).ToList();
        });
        return true;
    }

    private List<SqlContextOut> GetResponeFromDialogs(List<RoleDialogModel> dialogs)
    {
        var dialog = dialogs.Last(x => !x.Files.IsNullOrEmpty());
        var sqlCommandList = new List<SqlContextOut>();
        foreach (var file in dialog.Files)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.FileStorageUrl)) continue;

            string extension = Path.GetExtension(file.FileStorageUrl);
            if (!_excelMimeTypes.Contains(extension)) continue;

            _currentFileName = Path.GetFileName(file.FileStorageUrl);

            var bytes = _fileStorage.GetFileBytes(file.FileStorageUrl);
            var workbook = ConvertToWorkBook(bytes);

            var currentCommandList = _mySqlService.WriteExcelDataToDB(workbook);
            sqlCommandList.AddRange(currentCommandList);
        }
        return sqlCommandList;
    }

    private string GenerateSqlExecutionSummary(List<SqlContextOut> messageList)
    {
        var stringBuilder = new StringBuilder();
        if (messageList.Any(x => x.isSuccessful))
        {
            stringBuilder.Append("---Success---");
            stringBuilder.Append("\r\n");
            foreach (var message in messageList.Where(x => x.isSuccessful))
            {
                stringBuilder.Append(message.Message);
                stringBuilder.Append("\r\n\r\n");
            }
        }
        if (messageList.Any(x => !x.isSuccessful))
        {
            stringBuilder.Append("---Failed---");
            stringBuilder.Append("\r\n");
            foreach (var message in messageList.Where(x => !x.isSuccessful))
            {
                stringBuilder.Append(message.Message);
                stringBuilder.Append("\r\n");
            }
        }
        return stringBuilder.ToString();
    }

    private IWorkbook ConvertToWorkBook(byte[] bytes)
    {
        IWorkbook workbook;
        using (var fileStream = new MemoryStream(bytes))
        {
            workbook = new XSSFWorkbook(fileStream);
        }
        return workbook;
    }
    #endregion
}
