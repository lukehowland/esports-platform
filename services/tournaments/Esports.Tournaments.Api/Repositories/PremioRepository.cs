using Cassandra;
using Esports.Tournaments.Api.Cassandra;
using Esports.Tournaments.Api.Domain;
using Esports.Tournaments.Api.Dtos;

namespace Esports.Tournaments.Api.Repositories;

public interface IPremioRepository
{
    Task AsignarAsync(Premio p);
    Task<IEnumerable<PremioResponse>> ObtenerPorTorneoAsync(Guid torneoId);
    Task<IEnumerable<PremioEquipoResponse>> ObtenerPorEquipoAsync(Guid equipoId);
}

public class PremioRepository : IPremioRepository
{
    private readonly global::Cassandra.ISession _session;
    private readonly string _ks;
    private readonly PreparedStatement _insPorTorneo, _insPorEquipo;
    private readonly PreparedStatement _selPorTorneo, _selPorEquipo;

    public PremioRepository(ICassandraSession c, IConfiguration config)
    {
        _session = c.Session;
        _ks = config["Cassandra:Keyspace"] ?? "esports_tournaments";
        _insPorTorneo = _session.Prepare($@"INSERT INTO {_ks}.premios_por_torneo
            (torneo_id, monto, premio_id, tipo, equipo_id, nombre_equipo) VALUES (?, ?, ?, ?, ?, ?)");
        _insPorEquipo = _session.Prepare($@"INSERT INTO {_ks}.premios_por_equipo
            (equipo_id, monto, premio_id, torneo_id, nombre_torneo, tipo) VALUES (?, ?, ?, ?, ?, ?)");
        _selPorTorneo = _session.Prepare($"SELECT premio_id, torneo_id, monto, tipo, equipo_id, nombre_equipo FROM {_ks}.premios_por_torneo WHERE torneo_id = ?");
        _selPorEquipo = _session.Prepare($"SELECT premio_id, torneo_id, nombre_torneo, monto, tipo FROM {_ks}.premios_por_equipo WHERE equipo_id = ?");
    }

    public async Task AsignarAsync(Premio p)
    {
        // BATCH: premios_por_torneo + premios_por_equipo (solo si tiene equipo asignado)
        var batch = new BatchStatement();
        batch.Add(_insPorTorneo.Bind(p.TorneoId, p.Monto, p.PremioId, p.Tipo, p.EquipoId, p.NombreEquipo));
        if (p.EquipoId.HasValue)
            batch.Add(_insPorEquipo.Bind(p.EquipoId.Value, p.Monto, p.PremioId, p.TorneoId, p.NombreTorneo, p.Tipo));
        await _session.ExecuteAsync(batch);
    }

    public async Task<IEnumerable<PremioResponse>> ObtenerPorTorneoAsync(Guid torneoId)
    {
        var rows = await _session.ExecuteAsync(_selPorTorneo.Bind(torneoId));
        return rows.Select(r => new PremioResponse(
            r.GetValue<Guid>("premio_id"),
            r.GetValue<Guid>("torneo_id"),
            r.GetValue<decimal>("monto"),
            r.GetValue<string>("tipo"),
            r.IsNull("equipo_id") ? null : r.GetValue<Guid>("equipo_id"),
            r.IsNull("nombre_equipo") ? null : r.GetValue<string>("nombre_equipo")));
    }

    public async Task<IEnumerable<PremioEquipoResponse>> ObtenerPorEquipoAsync(Guid equipoId)
    {
        var rows = await _session.ExecuteAsync(_selPorEquipo.Bind(equipoId));
        return rows.Select(r => new PremioEquipoResponse(
            r.GetValue<Guid>("premio_id"),
            r.GetValue<Guid>("torneo_id"),
            r.GetValue<string>("nombre_torneo"),
            r.GetValue<decimal>("monto"),
            r.GetValue<string>("tipo")));
    }
}
