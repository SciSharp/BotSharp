namespace BotSharp.Abstraction.Files;

public interface IFileInstructService
{
    #region Image
    Task<RoleDialogModel> ReadImages(string? provider, string? model, string text, IEnumerable<BotSharpFile> images);
    Task<RoleDialogModel> GenerateImage(string? provider, string? model, string text);
    Task<RoleDialogModel> VaryImage(string? provider, string? model, BotSharpFile image);
    Task<RoleDialogModel> EditImage(string? provider, string? model, string text, BotSharpFile image);
    Task<RoleDialogModel> EditImage(string? provider, string? model, string text, BotSharpFile image, BotSharpFile mask);
    #endregion

    #region Pdf
    /// <summary>
    /// Take screenshots of pdf pages and get response from llm
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="files">Pdf files</param>
    /// <returns></returns>
    Task<string> ReadPdf(string? provider, string? model, string? modelId, string prompt, List<BotSharpFile> files);
    #endregion

    #region Select file
    Task<IEnumerable<MessageFileModel>> SelectMessageFiles(string conversationId, SelectFileOptions options);
    #endregion
}
