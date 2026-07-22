namespace LoginAnomaly.Domain.Detection.Rules;

public class VelocityRule : IDetectionRule
{
    public string Name => "Velocity (Rapid-fire)";

    private const int Score = 25;
    private const int AttemptThreshold = 10;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(10);

    public RuleResult Evaluate(LoginContext ctx)
    {
        var since = ctx.TimestampUtc - Window;

        // Hitung SEMUA percobaan (sukses+gagal) dari IP/username sama dalam jendela pendek
        var rapidCount = ctx.RecentHistory.Count(e =>
            e.TimestampUtc >= since &&
            (e.IpAddress == ctx.IpAddress || e.Username == ctx.Username));

        if (rapidCount >= AttemptThreshold)
            return new RuleResult(Score,
                $"{rapidCount} attempts within {Window.TotalSeconds:F0}s (seems like bot velocity)");

        return new RuleResult(0, null);
    }
}