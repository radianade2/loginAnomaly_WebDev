using LoginAnomaly.Domain.Entities;

namespace LoginAnomaly.Api.Auth;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}