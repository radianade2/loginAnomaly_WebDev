using LoginAnomaly.Domain.Entities;

namespace LoginAnomaly.Domain.Detection.Rules;

public class ImpossibleTravelRule : IDetectionRule
{
    public string Name => "Impossible Travel";

    private const int Score = 40;
    private const double MaxSpeedKmh = 900;  // kecepatan pesawat komersial biasanya

    public RuleResult Evaluate(LoginContext ctx)
    {
        if (!ctx.LoginSucceeded || ctx.Latitude is null || ctx.Longitude is null)
            return new RuleResult(0, null);

        var last = ctx.RecentHistory
            .Where(e => e.LoginSucceeded
                     && e.Username == ctx.Username
                     && e.Latitude.HasValue && e.Longitude.HasValue)
            .OrderByDescending(e => e.TimestampUtc)
            .FirstOrDefault();

        if (last is null) return new RuleResult(0, null);

        double km = Haversine(
            last.Latitude!.Value, last.Longitude!.Value,
            ctx.Latitude.Value, ctx.Longitude.Value);

        double hours = (ctx.TimestampUtc - last.TimestampUtc).TotalHours;
        if (hours <= 0) return new RuleResult(0, null);

        double speed = km / hours;
        if (speed > MaxSpeedKmh)
            return new RuleResult(Score,
                $"Implied speed {speed:F0} km/h ({km:F0} km in {hours:F2} h)");

        return new RuleResult(0, null);
    }

    // Jarak dua titik di permukaan bumi (km)
    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;  // radius bumi (km)
        double dLat = Deg2Rad(lat2 - lat1);
        double dLon = Deg2Rad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double Deg2Rad(double d) => d * Math.PI / 180;
}