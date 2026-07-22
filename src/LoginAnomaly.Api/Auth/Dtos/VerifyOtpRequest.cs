namespace LoginAnomaly.Api.Auth.Dtos;

public record VerifyOtpRequest(string Username, string OtpCode);