using LoginAnomaly.Domain.Entities;

namespace LoginAnomaly.Domain.Detection;

public class LoginContext
{
    public string Username { get; init; } = null!;
    public string IpAddress { get; init; } = null!;
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string DeviceFingerprint { get; init; } = null!;
    public DateTime TimestampUtc { get; init; }
    public bool LoginSucceeded { get; init; }

    // Riwayat yang sudah di-load service, jadi rule tak menyentuh DB langsung
    public IReadOnlyList<LoginEvent> RecentHistory { get; init; } = new List<LoginEvent>();
    public IReadOnlyList<KnownDevice> KnownDevices { get; init; } = new List<KnownDevice>();
}