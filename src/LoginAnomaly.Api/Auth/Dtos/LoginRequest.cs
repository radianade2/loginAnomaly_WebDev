namespace LoginAnomaly.Api.Auth.Dtos;

public record LoginRequest(
    string Username,
    string Password,
    string? IpAddress = null,
    double? Latitude = null,
    double? Longitude = null,
    string? DeviceFingerprint = null);