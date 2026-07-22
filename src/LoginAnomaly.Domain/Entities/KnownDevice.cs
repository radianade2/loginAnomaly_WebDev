namespace LoginAnomaly.Domain.Entities;

public class KnownDevice
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DeviceFingerprint { get; set; } = null!;
    public string? IpAddress { get; set; }
    public DateTime FirstSeenUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }

    public User User { get; set; } = null!;
}

// using System;

// namespace LoginAnomaly.Domain.Entities
// {
//     public class KnownDevice
//     {
//         public int Id { get; set; }
//         public int UserId { get; set; }
//         public string DeviceFingerprint { get; set; } = string.Empty;
//         public string IpAddress { get; set; } = string.Empty;
//         public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
//         public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;

//         // Navigation Property
//         public User? User { get; set; }
//     }
// }