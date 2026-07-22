using LoginAnomaly.Domain.Enums;

namespace LoginAnomaly.Domain.Entities;

public class LoginEvent
{
    public long Id { get; set; }

    public int? UserId { get; set; }            // NULLABLE — lihat catatan
    public string Username { get; set; } = null!;

    public string IpAddress { get; set; } = null!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string DeviceFingerprint { get; set; } = null!;

    public bool LoginSucceeded { get; set; }    // sumbu 1: kredensial
    public int RiskScore { get; set; }
    public RiskDecision Decision { get; set; }  // sumbu 2: risiko

    public bool IsSimulated { get; set; }
    public DateTime TimestampUtc { get; set; }

    // Navigasi
    public User? User { get; set; }
    public ICollection<RuleHit> RuleHits { get; set; } = new List<RuleHit>();
    public Alert? Alert { get; set; }           // 0..1
}
