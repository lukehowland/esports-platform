using Esports.Auth.Api.Domain;
using Esports.Auth.Api.Dtos;
using Esports.Auth.Api.Repositories;
using Esports.Auth.Api.Services;
using Esports.Auth.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Auth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private static readonly string[] RolesPermitidos =
    [
        AuthConstants.Roles.Admin,
        AuthConstants.Roles.Organizador,
        AuthConstants.Roles.Capitan,
        AuthConstants.Roles.Fan
    ];

    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;

    public AuthController(IUsuarioRepository usuarios, IPasswordService passwords, ITokenService tokens)
    {
        _usuarios = usuarios;
        _passwords = passwords;
        _tokens = tokens;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var usuario = await _usuarios.GetByUsernameAsync(request.Username);

        if (usuario is null || !_passwords.Verify(request.Password, usuario.PasswordHash))
            return Unauthorized(new ProblemDetails
            {
                Title = "Credenciales inválidas",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Username o contraseña incorrectos.",
            });

        var (token, expiresAt) = _tokens.GenerateToken(usuario);

        return Ok(new LoginResponse(
            Token: token,
            Rol: usuario.Rol,
            Nombre: usuario.NombreDisplay,
            OrganizadorId: usuario.OrganizadorId,
            EquipoId: usuario.EquipoId,
            ExpiraEn: expiresAt));
    }

    [HttpPost("register")]
    [Authorize(Roles = AuthConstants.Roles.Admin)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var username = request.Username.Trim();
        var rol = request.Rol.Trim().ToLowerInvariant();
        var nombreDisplay = request.NombreDisplay.Trim();

        var validationError = ValidateRegisterRequest(rol, request.OrganizadorId, request.EquipoId);
        if (validationError is not null)
            return validationError;

        var usuario = new Usuario
        {
            Username = username,
            PasswordHash = _passwords.Hash(request.Password),
            Rol = rol,
            OrganizadorId = request.OrganizadorId,
            EquipoId = request.EquipoId,
            NombreDisplay = nombreDisplay,
        };

        var created = await _usuarios.CreateAsync(usuario);
        if (!created)
            return Conflict(new ProblemDetails
            {
                Title = "Usuario ya existe",
                Status = StatusCodes.Status409Conflict,
                Detail = $"El usuario '{request.Username}' ya está registrado.",
            });

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new MeResponse(
            Username: User.Identity?.Name ?? string.Empty,
            Rol: User.GetRol() ?? string.Empty,
            Nombre: User.GetNombre(),
            OrganizadorId: User.GetOrganizadorId(),
            EquipoId: User.GetEquipoId()));
    }

    private IActionResult? ValidateRegisterRequest(string rol, Guid? organizadorId, Guid? equipoId)
    {
        var errors = new Dictionary<string, string[]>();

        if (!RolesPermitidos.Contains(rol, StringComparer.Ordinal))
        {
            errors[nameof(RegisterRequest.Rol)] =
            [
                $"Rol inválido. Valores permitidos: {string.Join(", ", RolesPermitidos)}."
            ];
        }

        if (rol == AuthConstants.Roles.Organizador)
        {
            if (!organizadorId.HasValue)
                errors[nameof(RegisterRequest.OrganizadorId)] = ["Un organizador requiere OrganizadorId."];
            if (equipoId.HasValue)
                errors[nameof(RegisterRequest.EquipoId)] = ["Un organizador no puede estar vinculado a EquipoId."];
        }
        else if (rol == AuthConstants.Roles.Capitan)
        {
            if (!equipoId.HasValue)
                errors[nameof(RegisterRequest.EquipoId)] = ["Un capitán requiere EquipoId."];
            if (organizadorId.HasValue)
                errors[nameof(RegisterRequest.OrganizadorId)] = ["Un capitán no puede estar vinculado a OrganizadorId."];
        }
        else if (rol == AuthConstants.Roles.Admin || rol == AuthConstants.Roles.Fan)
        {
            if (organizadorId.HasValue)
                errors[nameof(RegisterRequest.OrganizadorId)] = ["Este rol no puede estar vinculado a OrganizadorId."];
            if (equipoId.HasValue)
                errors[nameof(RegisterRequest.EquipoId)] = ["Este rol no puede estar vinculado a EquipoId."];
        }

        return errors.Count == 0
            ? null
            : BadRequest(new ValidationProblemDetails(errors)
            {
                Title = "Error de validación",
                Status = StatusCodes.Status400BadRequest
            });
    }
}
