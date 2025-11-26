using DeviceId;
using Microsoft.Extensions.Logging;
using System.Net.NetworkInformation;
using System.Security.Cryptography;

namespace BotSharp.Abstraction.Diagnostics.Telemetry;

public class MachineInformationProvider(ILogger<MachineInformationProvider> logger)
    : IMachineInformationProvider
{
    protected const string NotAvailable = "N/A";

    private static readonly SHA256 s_sHA256 = SHA256.Create();

    private readonly ILogger<MachineInformationProvider> _logger = logger;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public async Task<string?> GetOrCreateDeviceId()
    {
        string deviceId = new DeviceIdBuilder()
                  .AddMachineName()
                  .AddOsVersion()
                  .OnWindows(windows => windows
                      .AddProcessorId()
                      .AddMotherboardSerialNumber()
                      .AddSystemDriveSerialNumber())
                  .OnLinux(linux => linux
                      .AddMotherboardSerialNumber()
                      .AddSystemDriveSerialNumber())
                  .OnMac(mac => mac
                      .AddSystemDriveSerialNumber()
                      .AddPlatformSerialNumber())
                  .ToString();

        return deviceId;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual Task<string> GetMacAddressHash()
    {
        return Task.Run(() =>
        {
            try
            {
                var address = GetMacAddress();

                return address != null
                    ? HashValue(address)
                    : NotAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to calculate MAC address hash.");
                return NotAvailable;
            }
        });
    }

    /// <summary>
    /// Searches for first network interface card that is up and has a physical address.
    /// </summary>
    /// <returns>Hash of the MAC address or <see cref="NotAvailable"/> if none can be found.</returns>
    protected virtual string? GetMacAddress()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(x => x.GetPhysicalAddress().ToString())
            .FirstOrDefault(x => !string.IsNullOrEmpty(x));
    }

    /// <summary>
    /// Generates a SHA-256 of the given value.
    /// </summary>
    protected string HashValue(string value)
    {
        var hashInput = s_sHA256.ComputeHash(Encoding.UTF8.GetBytes(value));
        return BitConverter.ToString(hashInput).Replace("-", string.Empty).ToLowerInvariant();
    }
}
