using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Esports.Auth.Api.Domain;
using Esports.Auth.Shared;
using Microsoft.IdentityModel.Tokens;

namespace Esports.Auth.Api.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public (string Token, DateTimeOffset ExpiresAt) GenerateToken(Usuario usuario)
    {
        var secret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Falta la configuración Jwt:Secret.");
        var issuer = _config["Jwt:Issuer"] ?? "esports-auth";
        var audience = _config["Jwt:Audience"] ?? "esports-platform";
        var expiresHours = int.Parse(_config["Jwt:ExpiresHours"] ?? "8");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Username),
            new(AuthConstants.Claims.Username, usuario.Username),
            new(AuthConstants.Claims.Rol, usuario.Rol),
            new(AuthConstants.Claims.Nombre, usuario.NombreDisplay),
        };

        if (usuario.OrganizadorId.HasValue)
            claims.Add(new(AuthConstants.Claims.OrganizadorId, usuario.OrganizadorId.Value.ToString()));
        if (usuario.EquipoId.HasValue)
            claims.Add(new(AuthConstants.Claims.EquipoId, usuario.EquipoId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(expiresHours);

        var jwtToken = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(jwtToken), expiresAt);
    }
}
