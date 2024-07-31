namespace BotSharp.Abstraction.Repositories.Enums;

public static class FileStorageEnum
{
    public const string LocalFileStorage = nameof(LocalFileStorage);
    public const string AmazonS3Storage = nameof(AmazonS3Storage);
    public const string AzureBlobStorage = nameof(AzureBlobStorage);
    public const string TencentCosStorage = nameof(TencentCosStorage);
}
