using Esports.Auth.Api.Domain;

namespace Esports.Auth.Api.Services;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateToken(Usuario usuario);
}
