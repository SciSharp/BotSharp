namespace BotSharp.Plugin.TencentCos.Settings
{
    public class TencentCosSettings
    {
        public string AppId { get; set; }
        public string SecretId { get; set; }
        public string SecretKey { get; set; }
        public string Region { get; set; }
        public string BucketName { get; set; }
        public int KeyDurationSecond { get; set; } = 600;
    }
}
