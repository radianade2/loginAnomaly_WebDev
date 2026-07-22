using LoginAnomaly.Api.Auth;
using LoginAnomaly.Api.Auth.Dtos;
using LoginAnomaly.Domain.Entities;
using LoginAnomaly.Domain.Enums;
using LoginAnomaly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginAnomaly.Api.Controllers;

[ApiController]
[Route("api/auth")]

public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly LoginPipelineService _pipeline;

    public AuthController(AppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt, LoginPipelineService pipeline)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _pipeline = pipeline;
    }

    // REGISTER
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username dan password wajib diisi." });

        // Cek username sudah ada
        var exists = await _db.Users
            .AnyAsync(u => u.Username == request.Username);
        if (exists)
            return Conflict(new { message = "Username sudah terdaftar." });

        // Buat user baru dan password ter-hash
        var user = new User
        {
            Username = request.Username,
            PasswordHash = _hasher.Hash(request.Password),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Username });
    }

    // LOGIN
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var ip = request.IpAddress
            ?? HttpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
        var device = request.DeviceFingerprint
            ?? Request.Headers.UserAgent.ToString();

        var result = await _pipeline.ProcessAsync(
            request.Username, request.Password,
            ip, request.Latitude, request.Longitude,
            device, isSimulated: false);

        if (result.IsLockedOut && result.LockedUntilUtc is not null) {
            var remaining = (int)Math.Ceiling((result.LockedUntilUtc.Value - DateTime.UtcNow).TotalMinutes);
            return StatusCode(423, new
            {
                decision = "BLOCK",
                locked = true,
                remainingMinutes = remaining,
                message = $"Akun terkunci. Coba lagi dalam {remaining} menit."
            });
        }

        if (!result.LoginSucceeded)
            return Unauthorized(new { decision = result.Decision.ToString(), result.Score, message = "Username atau password salah." });

        return result.Decision switch {
            RiskDecision.Allow => Ok(new { decision = "ALLOW", result.Score, token = _jwt.GenerateToken(result.User!) }),
            RiskDecision.Challenge => StatusCode(403, new { decision = "CHALLENGE", result.Score, otp = result.OtpCode, message = "Verifikasi tambahan diperlukan." }),
            _                      => StatusCode(423, new { decision = "BLOCK", result.Score, message = "Akses diblokir." })
        };
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null)
            return Unauthorized(new { message = "User tidak ditemukan." });

        var now = DateTime.UtcNow;

        // Cek OTP: ada, cocok, belum kedaluwarsa
        if (user.OtpCode is null || user.OtpExpiresUtc is null || user.OtpExpiresUtc < now)
            return BadRequest(new { message = "OTP tidak berlaku atau sudah kedaluwarsa. Silakan login ulang." });

        if (user.OtpCode != request.OtpCode)
            return Unauthorized(new { message = "Kode OTP salah." });

        // OTP benar -> hangus (sekali pakai) & terbitkan token
        user.OtpCode = null;
        user.OtpExpiresUtc = null;
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return Ok(new { decision = "ALLOW", token, message = "Verifikasi berhasil." });
    }
}