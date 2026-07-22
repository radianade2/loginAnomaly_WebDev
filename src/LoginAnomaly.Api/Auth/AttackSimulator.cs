using LoginAnomaly.Domain.Enums;

namespace LoginAnomaly.Api.Auth;

public record SimulationResult(string Scenario, int EventsGenerated, string Note);

public class AttackSimulator
{
    private readonly LoginPipelineService _pipeline;

    public AttackSimulator(LoginPipelineService pipeline) => _pipeline = pipeline;

    public async Task<SimulationResult> RunAsync(string scenario, string targetUsername)
    {
        scenario = scenario.ToLowerInvariant();
        return scenario switch
        {
            "bruteforce"         => await BruteForce(targetUsername),
            "velocity"           => await Velocity(targetUsername),
            "impossible-travel"  => await ImpossibleTravel(targetUsername),
            _ => new SimulationResult(scenario, 0, "Unknown scenario.")
        };
    }

    // 8x password salah dari 1 IP -> picu Brute Force
    private async Task<SimulationResult> BruteForce(string username)
    {
        const string ip = "66.66.66.66";
        for (int i = 0; i < 8; i++)
        {
            await _pipeline.ProcessAsync(
                username, "wrong-password",
                ip, lat: null, lng: null,
                deviceFingerprint: "sim-bruteforce-bot",
                isSimulated: true);
        }
        return new SimulationResult("bruteforce", 8,
            "8 failed logins from a single IP.");
    }

    // 15x percobaan super cepat -> picu Velocity
    private async Task<SimulationResult> Velocity(string username)
    {
        const string ip = "77.77.77.77";
        for (int i = 0; i < 15; i++)
        {
            await _pipeline.ProcessAsync(
                username, "wrong-password",
                ip, lat: null, lng: null,
                deviceFingerprint: "sim-velocity-bot",
                isSimulated: true);
        }
        return new SimulationResult("velocity", 15,
            "15 rapid-fire attempts within seconds.");
    }

    // 2 login sukses berjauhan -> picu Impossible Travel
    private async Task<SimulationResult> ImpossibleTravel(string username)
    {
        // Catatan: butuh password BENAR agar login sukses. Untuk simulasi,
        // skenario ini mengasumsikan attacker sudah punya kredensial valid.
        await _pipeline.ProcessAsync(username, "rahasia123",
            "88.0.0.1", lat: -6.2, lng: 106.8,
            deviceFingerprint: "sim-traveler", isSimulated: true);   // Jakarta

        await _pipeline.ProcessAsync(username, "rahasia123",
            "88.0.0.2", lat: 51.5, lng: -0.12,
            deviceFingerprint: "sim-traveler", isSimulated: true);   // London

        return new SimulationResult("impossible-travel", 2,
            "Two successful logins from Jakarta then London.");
    }
}