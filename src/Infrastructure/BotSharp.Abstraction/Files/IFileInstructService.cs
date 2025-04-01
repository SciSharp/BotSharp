using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Files;

public interface IFileInstructService
{
    #region Image
    Task<string> ReadImages(string text, IEnumerable<InstructFileModel> images, InstructOptions? options = null);
    Task<RoleDialogModel> GenerateImage(string text, InstructOptions? options = null);
    Task<RoleDialogModel> VaryImage(InstructFileModel image, InstructOptions? options = null);
    Task<RoleDialogModel> EditImage(string text, InstructFileModel image, InstructOptions? options = null);
    Task<RoleDialogModel> EditImage(string text, InstructFileModel image, InstructFileModel mask, InstructOptions? options = null);
    #endregion

    #region Pdf
    /// <summary>
    /// Take screenshots of pdf pages and get response from llm
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="files">Pdf files</param>
    /// <returns></returns>
    Task<string> ReadPdf(string text, List<InstructFileModel> files, InstructOptions? options = null);
    #endregion

    #region Audio
    Task<string> SpeechToText(InstructFileModel audio, string? text = null, InstructOptions? options = null);
    #endregion

    #region Select file
    Task<IEnumerable<MessageFileModel>> SelectMessageFiles(string conversationId, SelectFileOptions options);
    #endregion
}
