using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Esports.Auth.Shared;

/// <summary>
/// Configuración compartida de validación de JWT. Cada microservicio que protege
/// mutaciones la invoca para validar el token por sí mismo (modelo distribuido,
/// zero-trust). El secret/issuer/audience llegan por variables de entorno (Jwt__*).
/// </summary>
public static class JwtAuthExtensions
{
    public static IServiceCollection AddEsportsJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Falta la configuración Jwt:Secret (env Jwt__Secret).");
        var issuer = config["Jwt:Issuer"] ?? "esports-auth";
        var audience = config["Jwt:Audience"] ?? "esports-platform";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = AuthConstants.Claims.Username,
                    // El rol viaja en el claim custom "rol"; mapearlo al ClaimsPrincipal.IsInRole
                    RoleClaimType = AuthConstants.Claims.Rol,
                };
            });

        services.AddAuthorization();
        return services;
    }
}
