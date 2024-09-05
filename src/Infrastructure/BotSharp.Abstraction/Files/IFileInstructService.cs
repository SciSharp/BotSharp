namespace BotSharp.Abstraction.Files;

public interface IFileInstructService
{
    #region Image
    Task<string> ReadImages(string? provider, string? model, string text, IEnumerable<InstructFileModel> images);
    Task<RoleDialogModel> GenerateImage(string? provider, string? model, string text);
    Task<RoleDialogModel> VaryImage(string? provider, string? model, InstructFileModel image);
    Task<RoleDialogModel> EditImage(string? provider, string? model, string text, InstructFileModel image);
    Task<RoleDialogModel> EditImage(string? provider, string? model, string text, InstructFileModel image, InstructFileModel mask);
    #endregion

    #region Pdf
    /// <summary>
    /// Take screenshots of pdf pages and get response from llm
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="files">Pdf files</param>
    /// <returns></returns>
    Task<string> ReadPdf(string? provider, string? model, string? modelId, string prompt, List<InstructFileModel> files);
    #endregion

    #region Audio
    Task<string> SpeechToText(string? provider, string? model, InstructFileModel audio, string? text = null);
    #endregion

    #region Select file
    Task<IEnumerable<MessageFileModel>> SelectMessageFiles(string conversationId, SelectFileOptions options);
    #endregion
}
