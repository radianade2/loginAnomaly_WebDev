using LoginAnomaly.Domain.Entities;

namespace LoginAnomaly.Domain.Detection.Rules;

public class NewDeviceRule : IDetectionRule
{
    public string Name => "New Device / New IP";

    private const int Score = 20;

    public RuleResult Evaluate(LoginContext ctx)
    {
        // Cek whitelist: apakah fingerprint ini sudah dikenal untuk user tsb?
        bool known = ctx.KnownDevices.Any(d =>
            d.DeviceFingerprint == ctx.DeviceFingerprint);

        // Tandai hanya jika login sukses TAPI device belum di-whitelist
        if (ctx.LoginSucceeded && !known)
            return new RuleResult(Score,
                $"First successful login from unrecognized device");

        return new RuleResult(0, null);
    }
}