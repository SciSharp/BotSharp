using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Instructs.Options;
using BotSharp.OpenAPI.ViewModels.Instructs;

namespace BotSharp.OpenAPI.Controllers;

public partial class InstructModeController
{
    #region Pdf
    [HttpPost("/instruct/pdf-completion")]
    public async Task<PdfCompletionViewModel> PdfCompletion([FromBody] PdfReadFileRequest request)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        request.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var viewModel = new PdfCompletionViewModel();

        try
        {
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var content = await fileInstruct.ReadPdf(request.Text, request.Files, new InstructOptions
            {
                Provider = request.Provider,
                Model = request.Model,
                AgentId = request.AgentId,
                TemplateName = request.TemplateName,
                ImageConvertProvider = request.ImageConvertProvider
            });

            viewModel.Success = true;
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in pdf completion. {ex.Message}";
            _logger.LogError(ex, error);
            viewModel.ErrorMsg = error;
            return viewModel;
        }
    }

    [HttpPost("/instruct/pdf-completion/form")]
    public async Task<PdfCompletionViewModel> PdfCompletion([FromForm] IEnumerable<IFormFile> files, [FromForm] PdfReadRequest request)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        request?.States?.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var viewModel = new PdfCompletionViewModel();

        try
        {
            var fileModels = files.Select(x => new InstructFileModel
            {
                FileData = FileUtility.BuildFileDataFromFile(x),
                ContentType = x.ContentType
            }).ToList();

            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var content = await fileInstruct.ReadPdf(request?.Text ?? string.Empty, fileModels, new InstructOptions
            {
                Provider = request?.Provider,
                Model = request?.Model,
                AgentId = request?.AgentId,
                TemplateName = request?.TemplateName,
                ImageConvertProvider = request?.ImageConvertProvider
            });

            viewModel.Success = true;
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in pdf completion. {ex.Message}";
            _logger.LogError(ex, error);
            viewModel.ErrorMsg = error;
            return viewModel;
        }
    }
    #endregion
}
