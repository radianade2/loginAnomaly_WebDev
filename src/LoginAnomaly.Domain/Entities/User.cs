namespace LoginAnomaly.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime? LockedUntilUtc { get; set; }
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiresUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    // Navigasi: satu user punya banyak login & banyak device dikenal
    public ICollection<LoginEvent> LoginEvents { get; set; } = new List<LoginEvent>();
    public ICollection<KnownDevice> KnownDevices { get; set; } = new List<KnownDevice>();
}

// using System;
// using System.Collections.Generic;

// namespace LoginAnomaly.Domain.Entities
// {
//     public class User
//     {
//         public int Id { get; set; }
//         public string Username { get; set; } = string.Empty;
//         public string PasswordHash { get; set; } = string.Empty;
//         public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

//         // Navigation Properties
//         public ICollection<KnownDevice> KnownDevices { get; set; } = new List<KnownDevice>();
//         public ICollection<LoginEvent> LoginEvents { get; set; } = new List<LoginEvent>();
//     }
// }