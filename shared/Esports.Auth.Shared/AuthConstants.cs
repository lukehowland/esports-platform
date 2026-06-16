namespace Esports.Auth.Shared;

/// <summary>
/// Nombres de roles y de claims usados en toda la plataforma. El servicio auth los
/// emite en el JWT; los servicios de dominio los leen para autorizar.
/// </summary>
public static class AuthConstants
{
    public static class Roles
    {
        public const string Admin = "admin";
        public const string Organizador = "organizador";
        public const string Capitan = "capitan";
        public const string Fan = "fan";
    }

    public static class Claims
    {
        public const string Username = "username";
        public const string Rol = "rol";
        public const string OrganizadorId = "organizador_id";
        public const string EquipoId = "equipo_id";
        public const string Nombre = "nombre";
    }

    /// <summary>Esquema de autenticación por defecto (JWT Bearer).</summary>
    public const string Scheme = "Bearer";
}
