using LoginAnomaly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginAnomaly.Api.Controllers;

[ApiController]
[Route("api")]
public class MonitoringController : ControllerBase
{
    private readonly AppDbContext _db;

    public MonitoringController(AppDbContext db) => _db = db;

    // GET /api/events?take=50
    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] int take = 50)
    {
        var events = await _db.LoginEvents
            .OrderByDescending(e => e.Id)
            .Take(take)
            .Select(e => new
            {
                e.Id,
                e.Username,
                e.IpAddress,
                e.Latitude,
                e.Longitude,
                e.LoginSucceeded,
                e.RiskScore,
                Decision = e.Decision.ToString(),
                e.IsSimulated,
                e.TimestampUtc
            })
            .ToListAsync();

        return Ok(events);
    }

    // GET /api/events/{id} -> detail + rule hits
    [HttpGet("events/{id:long}")]
    public async Task<IActionResult> GetEventDetail(long id)
    {
        var ev = await _db.LoginEvents
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.Username,
                e.IpAddress,
                e.Latitude,
                e.Longitude,
                e.LoginSucceeded,
                e.RiskScore,
                Decision = e.Decision.ToString(),
                e.IsSimulated,
                e.TimestampUtc,
                Rules = e.RuleHits.Select(r => new { r.RuleName, r.Score, r.Reason })
            })
            .FirstOrDefaultAsync();

        if (ev is null) return NotFound();
        return Ok(ev);
    }

    // GET /api/alerts?onlyActive=true
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts([FromQuery] bool onlyActive = false)
    {
        var query = _db.Alerts.AsQueryable();
        if (onlyActive)
            query = query.Where(a => !a.IsAcknowledged);

        var alerts = await query
            .OrderByDescending(a => a.Id)
            .Select(a => new
            {
                a.Id,
                a.LoginEventId,
                Severity = a.Severity.ToString(),
                a.Summary,
                a.IsAcknowledged,
                a.AcknowledgedAtUtc,
                Username = a.LoginEvent.Username,
                a.LoginEvent.RiskScore,
                a.LoginEvent.TimestampUtc
            })
            .ToListAsync();

        return Ok(alerts);
    }

    [HttpPost("alerts/{id:long}/ack")]
    public async Task<IActionResult> Acknowledge(long id)
    {
        var alert = await _db.Alerts
            .Include(a => a.LoginEvent)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (alert is null) return NotFound();

        alert.IsAcknowledged = true;
        alert.AcknowledgedAtUtc = DateTime.UtcNow;

        var username = alert.LoginEvent.Username;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is not null && user.LockedUntilUtc is not null)
        {
            user.LockedUntilUtc = null;
        }

        await _db.SaveChangesAsync();
        return Ok(new { alert.Id, alert.IsAcknowledged, alert.AcknowledgedAtUtc, unlockedUser = username });
    }

    // GET /api/stats/summary -> angka untuk kartu dashboard
    [HttpGet("stats/summary")]
    public async Task<IActionResult> GetSummary()
    {
        var total = await _db.LoginEvents.CountAsync();
        var suspicious = await _db.LoginEvents.CountAsync(e => e.RiskScore >= 25);
        var activeAlerts = await _db.Alerts.CountAsync(a => !a.IsAcknowledged);

        return Ok(new { total, suspicious, activeAlerts });
    }
}