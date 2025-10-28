using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Instructs.Options;
using BotSharp.Core.Infrastructures;
using BotSharp.OpenAPI.ViewModels.Instructs;

namespace BotSharp.OpenAPI.Controllers;

public partial class InstructModeController
{
    #region Audio
    [HttpPost("/instruct/speech-to-text")]
    public async Task<SpeechToTextViewModel> SpeechToText([FromBody] SpeechToTextFileRequest request)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        request.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var viewModel = new SpeechToTextViewModel();

        try
        {
            var audio = request.File;
            if (audio == null)
            {
                return new SpeechToTextViewModel { Message = "Error! Cannot find a valid audio file!" };
            }
            var content = await fileInstruct.SpeechToText(audio, request.Text, new InstructOptions
            {
                Provider = request.Provider,
                Model = request.Model,
                AgentId = request.AgentId,
                TemplateName = request.TemplateName
            });
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in speech to text. {ex.Message}";
            _logger.LogError(ex, error);
            viewModel.Message = error;
            return viewModel;
        }
    }

    [HttpPost("/instruct/speech-to-text/form")]
    public async Task<SpeechToTextViewModel> SpeechToText(IFormFile file, [FromForm] SpeechToTextRequest request)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        request?.States?.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var viewModel = new SpeechToTextViewModel();

        try
        {
            var audioData = FileUtility.BuildFileDataFromFile(file);
            var content = await fileInstruct.SpeechToText(new InstructFileModel
            {
                FileData = audioData,
                FileName = Path.GetFileNameWithoutExtension(file.FileName),
                FileExtension = Path.GetExtension(file.FileName)
            },
            request?.Text ?? string.Empty,
            new InstructOptions
            {
                Provider = request?.Provider,
                Model = request?.Model,
                AgentId = request?.AgentId,
                TemplateName = request?.TemplateName
            });

            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in speech-to-text. {ex.Message}";
            _logger.LogError(ex, error);
            viewModel.Message = error;
            return viewModel;
        }
    }

    [HttpPost("/instruct/text-to-speech")]
    public async Task<IActionResult> TextToSpeech([FromBody] TextToSpeechRequest request)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        request.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));

        var completion = CompletionProvider.GetAudioSynthesizer(_services, provider: request.Provider, model: request.Model);
        var binaryData = await completion.GenerateAudioAsync(request.Text);
        var stream = binaryData.ToStream();
        stream.Position = 0;

        return new FileStreamResult(stream, "audio/mpeg") { FileDownloadName = "output.mp3" };
    }
    #endregion
}
