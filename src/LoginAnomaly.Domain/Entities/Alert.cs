using LoginAnomaly.Domain.Enums;

namespace LoginAnomaly.Domain.Entities;

public class Alert
{
    public long Id { get; set; }
    public long LoginEventId { get; set; }          // FK unik → relasi 1:0..1
    
    public AlertSeverity Severity { get; set; }
    public string Summary { get; set; } = null!;
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; } // null sampai di-ack

    public LoginEvent LoginEvent { get; set; } = null!;
}

// using System;

// namespace LoginAnomaly.Domain.Entities
// {
//     public enum AlertSeverity
//     {
//         Low = 0,
//         Medium = 1,
//         High = 2
//     }

//     public class Alert
//     {
//         public long Id { get; set; }
//         public long LoginEventId { get; set; }
//         public AlertSeverity Severity { get; set; } = AlertSeverity.Low; // LOW, MEDIUM, HIGH
//         public string Summary { get; set; } = string.Empty;
//         public bool IsAcknowledged { get; set; } = false;
//         public DateTime? AcknowledgedAtUtc { get; set; }

//         // Navigation Property
//         public LoginEvent? LoginEvent { get; set; }
//     }
// }