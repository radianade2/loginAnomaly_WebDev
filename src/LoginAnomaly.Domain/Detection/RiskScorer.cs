using LoginAnomaly.Domain.Enums;

namespace LoginAnomaly.Domain.Detection;

public record ScoringResult(
    int TotalScore,
    RiskDecision Decision,
    IReadOnlyList<(string RuleName, int Score, string? Reason)> Hits);

public class RiskScorer
{
    private readonly IEnumerable<IDetectionRule> _rules;

    public RiskScorer(IEnumerable<IDetectionRule> rules) => _rules = rules;

    public ScoringResult Evaluate(LoginContext ctx)
    {
        var hits = new List<(string, int, string?)>();
        int total = 0;

        // D.4: evaluasi sekuensial, satu siklus
        foreach (var rule in _rules)
        {
            var r = rule.Evaluate(ctx);
            if (r.Score > 0)
            {
                total += r.Score;
                hits.Add((rule.Name, r.Score, r.Reason));
            }
        }

        var decision = total >= 50 ? RiskDecision.Block
                     : total >= 25 ? RiskDecision.Challenge
                     : RiskDecision.Allow;

        return new ScoringResult(total, decision, hits);
    }
}