namespace LoginAnomaly.Domain.Entities;

public class RuleHit
{
    public long Id { get; set; }

    public long LoginEventId { get; set; }          // FK ke LoginEvent
    public string RuleName { get; set; } = null!;
    public int Score { get; set; }
    public string? Reason { get; set; }             // boleh kosong

    // Navigasi balik ke induk
    public LoginEvent LoginEvent { get; set; } = null!;
}

// namespace LoginAnomaly.Domain.Entities
// {
//     public class RuleHit
//     {
//         public long Id { get; set; }
//         public long LoginEventId { get; set; }
//         public string RuleName { get; set; } = string.Empty;
//         public int Score { get; set; }
//         public string Reason { get; set; } = string.Empty;

//         // Navigation Property
//         public LoginEvent? LoginEvent { get; set; }
//     }
// }