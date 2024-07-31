using BotSharp.Plugin.TencentCos.Modules;
using BotSharp.Plugin.TencentCos.Settings;
using COSXML;
using COSXML.Auth;

namespace BotSharp.Plugin.TencentCos
{
    public class TencentCosClient
    {
        public BucketClient BucketClient { get; private set; }
        public TencentCosClient(TencentCosSettings settings)
        {
            var cosXmlConfig = new CosXmlConfig.Builder()
                 .IsHttps(true)
                 .SetAppid(settings.AppId)
                 .SetRegion(settings.Region)
                 .Build();
            var cosCredentialProvider = new DefaultQCloudCredentialProvider(
                 settings.SecretId, settings.SecretKey, settings.KeyDurationSecond);

            var cosXml = new CosXmlServer(cosXmlConfig, cosCredentialProvider);

            BucketClient = new BucketClient(cosXml, $"{settings.BucketName}-{settings.AppId}", settings.AppId, settings.Region);
        }
    }
}
