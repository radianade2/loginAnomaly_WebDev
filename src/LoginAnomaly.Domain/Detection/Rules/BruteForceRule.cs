using LoginAnomaly.Domain.Entities;

namespace LoginAnomaly.Domain.Detection.Rules;

public class BruteForceRule : IDetectionRule
{
    public string Name => "Brute Force";

    private const int Score = 30;
    private const int FailThreshold = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public RuleResult Evaluate(LoginContext ctx)
    {
        var since = ctx.TimestampUtc - Window;

        // Hitung login GAGAL dari IP atau username yang sama dalam jendela waktu
        var recentFailures = ctx.RecentHistory.Count(e =>
            !e.LoginSucceeded &&
            e.TimestampUtc >= since &&
            (e.IpAddress == ctx.IpAddress || e.Username == ctx.Username));

        if (recentFailures >= FailThreshold)
            return new RuleResult(Score,
                $"{recentFailures} failed attempts within {Window.TotalSeconds:F0}s");

        return new RuleResult(0, null);
    }
}