namespace LoginAnomaly.Domain.Detection;

public interface IDetectionRule
{
    string Name { get; }
    RuleResult Evaluate(LoginContext ctx);
}