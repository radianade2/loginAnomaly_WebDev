using LoginAnomaly.Domain.Detection;
using LoginAnomaly.Domain.Entities;
using LoginAnomaly.Domain.Enums;
using LoginAnomaly.Infrastructure.Persistence;
using LoginAnomaly.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LoginAnomaly.Api.Auth;

public record PipelineResult(
    bool LoginSucceeded, 
    RiskDecision Decision, 
    int Score, 
    User? User,
    bool IsLockedOut = false,          // true kalau ditolak karena masih terkunci
    DateTime? LockedUntilUtc = null,   // kapan lock berakhir (untuk hitung sisa waktu)
    string? OtpCode = null
);

public class LoginPipelineService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly RiskScorer _scorer;
    private readonly IHubContext<MonitoringHub> _hub;

    private const int BlockMinutes = 15;
    private const int OtpValidMinutes = 2;
    // Threshold Score for Challenge (Alert) and Block
    private const int AlertThreshold = 25;
    private const int BlockThreshold = 50;


    public LoginPipelineService(AppDbContext db, IPasswordHasher hasher, RiskScorer scorer, IHubContext<MonitoringHub> hub)
    {
        _db = db;
        _hasher = hasher;
        _scorer = scorer;
        _hub = hub;
    }

    public async Task<PipelineResult> ProcessAsync(
        string username, string password,
        string ip, double? lat, double? lng,
        string deviceFingerprint, bool isSimulated)
    {
        var now = DateTime.UtcNow;

        // 1. Kredensial
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is not null && user.LockedUntilUtc is not null) {
            if (user.LockedUntilUtc > now)
            {
                return new PipelineResult(
                    LoginSucceeded: false,
                    Decision: RiskDecision.Block,
                    Score: 0,
                    User: null,
                    IsLockedOut: true,
                    LockedUntilUtc: user.LockedUntilUtc
                );
            }
            else
            {
                // Reset lockout
                user.LockedUntilUtc = null;
                await _db.SaveChangesAsync();
            }
        }
        bool succeeded = user is not null && _hasher.Verify(password, user.PasswordHash);

        // 2. Load riwayat untuk rule (cukup luas: 5 menit terakhir per IP/username)
        var since = now.AddMinutes(-5);
        var history = await _db.LoginEvents
            .Where(e => e.TimestampUtc >= since &&
                        (e.IpAddress == ip || e.Username == username))
            .OrderByDescending(e => e.TimestampUtc)
            .ToListAsync();
        
        var knownDevices = user is null
            ? new List<KnownDevice>()
            : await _db.KnownDevices.Where(d => d.UserId == user.Id).ToListAsync();

        // 3. Bangun konteks & jalankan scorer (sumbu 2: risiko)
        var ctx = new LoginContext
        {
            Username = username,
            IpAddress = ip,
            Latitude = lat,
            Longitude = lng,
            DeviceFingerprint = deviceFingerprint,
            TimestampUtc = now,
            LoginSucceeded = succeeded,
            RecentHistory = history,
            KnownDevices = knownDevices
        };
        var scoring = _scorer.Evaluate(ctx);

        // 4. D.1: simpan SEBELUM return
        var loginEvent = new LoginEvent
        {
            UserId = user?.Id,
            Username = username,
            IpAddress = ip,
            Latitude = lat,
            Longitude = lng,
            DeviceFingerprint = deviceFingerprint,
            LoginSucceeded = succeeded,
            RiskScore = scoring.TotalScore,
            Decision = scoring.Decision,
            IsSimulated = isSimulated,
            TimestampUtc = now
        };
        _db.LoginEvents.Add(loginEvent);
        await _db.SaveChangesAsync();   // simpan dulu supaya dapat Id

        // Whitelist device baru setelah login sukses
        if (succeeded && user is not null &&
            !knownDevices.Any(d => d.DeviceFingerprint == deviceFingerprint))
        {
            _db.KnownDevices.Add(new KnownDevice
            {
                UserId = user.Id,
                DeviceFingerprint = deviceFingerprint,
                IpAddress = ip,
                FirstSeenUtc = now,
                LastSeenUtc = now
            });
            await _db.SaveChangesAsync();
        }

        // RuleHit untuk tiap aturan yang menyala
        foreach (var (ruleName, score, reason) in scoring.Hits)
        {
            _db.RuleHits.Add(new RuleHit
            {
                LoginEventId = loginEvent.Id,
                RuleName = ruleName,
                Score = score,
                Reason = reason
            });
        }

        if (scoring.TotalScore >= AlertThreshold)
        {
            var severity = scoring.TotalScore >= BlockThreshold ? AlertSeverity.High : AlertSeverity.Medium;
            _db.Alerts.Add(new Alert
            {
                LoginEventId = loginEvent.Id,
                Severity = severity,
                Summary = string.Join("; ", scoring.Hits.Select(h => h.RuleName)),
                IsAcknowledged = false
            });
        }

        await _db.SaveChangesAsync();
        
        // SignalR broadcast
        // Siarkan event ke semua dashboard yang terhubung
        await _hub.Clients.All.SendAsync("loginEvent", new
        {
            loginEvent.Id,
            loginEvent.Username,
            loginEvent.IpAddress,
            loginEvent.LoginSucceeded,
            loginEvent.RiskScore,
            Decision = loginEvent.Decision.ToString(),
            loginEvent.IsSimulated,
            loginEvent.TimestampUtc
        });

        if (scoring.TotalScore >= AlertThreshold)
        {
            var severity = scoring.TotalScore >= BlockThreshold ? "High" : "Medium";
            await _hub.Clients.All.SendAsync("alert", new
            {
                loginEvent.Id,
                loginEvent.Username,
                Severity = severity,
                Summary = string.Join("; ", scoring.Hits.Select(h => h.RuleName)),
                loginEvent.RiskScore,
                IsAcknowledged = false,
            });
        }

        string? otpToReturn = null;
        if (scoring.Decision == RiskDecision.Block && user is not null)
        {
            user.LockedUntilUtc = now.AddMinutes(BlockMinutes);
            await _db.SaveChangesAsync();
        }
        else if (scoring.Decision == RiskDecision.Challenge && user is not null && succeeded)
        {
            // Generate OTP baru (6 digit), berlaku 2 menit
            otpToReturn = Random.Shared.Next(100000, 999999).ToString();
            user.OtpCode = otpToReturn;
            user.OtpExpiresUtc = now.AddMinutes(OtpValidMinutes);
            await _db.SaveChangesAsync();
        }
        else if (scoring.Decision == RiskDecision.Allow && user is not null && user.OtpCode is not null)
        {
            // Bersihkan OTP lama yang menggantung kalau sekarang sudah aman
            user.OtpCode = null;
            user.OtpExpiresUtc = null;
            await _db.SaveChangesAsync();
        }

        return new PipelineResult(
            LoginSucceeded: succeeded,
            Decision: scoring.Decision,
            Score: scoring.TotalScore,
            User: user,
            OtpCode: otpToReturn
        );
    }
}