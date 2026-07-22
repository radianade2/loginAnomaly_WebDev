namespace LoginAnomaly.Domain.Detection;

public record RuleResult(int Score, string? Reason);