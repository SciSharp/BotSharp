namespace BotSharp.Abstraction.Conversations.Models;

public class Attachment
{
    public string ContentType { get; set; }

    //
    // Summary:
    //     Gets the raw Content-Disposition header of the uploaded file.
    public string ContentDisposition { get; set; }

    //
    // Summary:
    //     Gets the file length in bytes.
    public long Length { get; set; }

    //
    // Summary:
    //     Gets the file name from the Content-Disposition header.
    public string FileName { get; set; }
}
