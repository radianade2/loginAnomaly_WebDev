namespace LoginAnomaly.Domain.Detection.Rules;

public class UnusualTimeRule : IDetectionRule
{
    public string Name => "Unusual Time";

    private const int Score = 15;
    private const int NightStartHour = 1;   // 01:00
    private const int NightEndHour = 5;     // 05:00

    public RuleResult Evaluate(LoginContext ctx)
    {
        int hour = ctx.TimestampUtc.Hour;

        // Jam dini hari dianggap tak biasa (versi sederhana)
        if (hour >= NightStartHour && hour < NightEndHour)
            return new RuleResult(Score,
                $"Login at unusual hour ({hour:00}:00 UTC)");

        return new RuleResult(0, null);
    }
}