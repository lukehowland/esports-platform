using Esports.Matches.Api.Domain;
using Esports.Matches.Api.Dtos;
using Esports.Matches.Api.Repositories;
using Esports.Shared.Events;
using MassTransit;

namespace Esports.Matches.Api.Services;

public interface IPartidaService
{
    Task<PartidaResponse> RegistrarAsync(RegistrarPartidaRequest req);
    Task<PartidaResponse?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<PartidaPorTorneoResponse>> ObtenerPorTorneoAsync(Guid torneoId);
    Task<IEnumerable<PartidaPorEquipoResponse>> ObtenerPorEquipoAsync(Guid equipoId);
    Task<IEnumerable<PartidaPorFechaResponse>> ObtenerPorFechaAsync(string dia);
    Task<IEnumerable<PartidaPorRivalesResponse>> ObtenerPorRivalesAsync(Guid equipoId, Guid rivalId);
    LiveMatchResponse ObtenerEnVivoDestacada(int? elapsedSeconds = null);
}

public class PartidaService : IPartidaService
{
    private readonly IPartidaRepository _repo;
    private readonly IPublishEndpoint _bus;

    public PartidaService(IPartidaRepository repo, IPublishEndpoint bus)
    {
        _repo = repo;
        _bus = bus;
    }

    public async Task<PartidaResponse> RegistrarAsync(RegistrarPartidaRequest req)
    {
        if (req.EquipoLocalId == Guid.Empty || req.EquipoVisitanteId == Guid.Empty || req.EquipoGanadorId == Guid.Empty)
            throw new ArgumentException("Los ids de equipos no pueden ser vacios.");

        if (req.EquipoLocalId == req.EquipoVisitanteId)
            throw new ArgumentException("El equipo local y el visitante deben ser distintos.");

        if (req.EquipoGanadorId != req.EquipoLocalId && req.EquipoGanadorId != req.EquipoVisitanteId)
            throw new ArgumentException("El ganador debe ser el equipo local o el equipo visitante.");

        var p = new Partida
        {
            PartidaId = Guid.NewGuid(),
            TorneoId = req.TorneoId,
            NombreTorneo = req.NombreTorneo.Trim(),
            Fecha = req.Fecha,
            EquipoLocalId = req.EquipoLocalId,
            EquipoVisitanteId = req.EquipoVisitanteId,
            NombreLocal = req.NombreLocal.Trim(),
            NombreVisitante = req.NombreVisitante.Trim(),
            EquipoGanadorId = req.EquipoGanadorId,
            Resultado = req.Resultado.Trim()
        };

        await _repo.RegistrarAsync(p);

        await _bus.Publish(new MatchPlayed(
            p.PartidaId, p.TorneoId,
            p.EquipoLocalId, p.EquipoVisitanteId, p.EquipoGanadorId,
            p.Fecha));

        return new PartidaResponse(p.PartidaId, p.TorneoId, p.NombreTorneo, p.Fecha,
            p.EquipoLocalId, p.EquipoVisitanteId, p.NombreLocal, p.NombreVisitante,
            p.EquipoGanadorId, p.Resultado);
    }

    public Task<PartidaResponse?> ObtenerPorIdAsync(Guid id)
        => _repo.ObtenerPorIdAsync(id);

    public Task<IEnumerable<PartidaPorTorneoResponse>> ObtenerPorTorneoAsync(Guid torneoId)
        => _repo.ObtenerPorTorneoAsync(torneoId);

    public Task<IEnumerable<PartidaPorEquipoResponse>> ObtenerPorEquipoAsync(Guid equipoId)
        => _repo.ObtenerPorEquipoAsync(equipoId);

    public async Task<IEnumerable<PartidaPorFechaResponse>> ObtenerPorFechaAsync(string dia)
    {
        var parts = dia.Split('-');
        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out var y) ||
            !int.TryParse(parts[1], out var m) ||
            !int.TryParse(parts[2], out var d))
            throw new ArgumentException($"Formato de fecha inválido: '{dia}'. Use YYYY-MM-DD.");

        var localDate = new global::Cassandra.LocalDate(y, m, d);
        return await _repo.ObtenerPorFechaAsync(localDate);
    }

    public Task<IEnumerable<PartidaPorRivalesResponse>> ObtenerPorRivalesAsync(Guid equipoId, Guid rivalId)
        => _repo.ObtenerPorRivalesAsync(equipoId, rivalId);

    public LiveMatchResponse ObtenerEnVivoDestacada(int? elapsedSeconds = null)
    {
        const int durationSeconds = 30 * 60;

        var elapsed = elapsedSeconds.HasValue
            ? Math.Clamp(elapsedSeconds.Value, 0, durationSeconds)
            : (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % (durationSeconds + 1));

        var moments = LiveMoments.Where(m => m.Second <= elapsed).ToArray();
        var objectives = LiveObjectives.Where(o => o.Second <= elapsed).ToArray();

        var localKills = moments.Sum(m => m.LocalKills);
        var visitorKills = moments.Sum(m => m.VisitorKills);

        var localDragons = objectives.Count(o => o.TeamTag == "T1" && o.Type == "dragon");
        var visitorDragons = objectives.Count(o => o.TeamTag == "GEN" && o.Type == "dragon");
        var localBarons = objectives.Count(o => o.TeamTag == "T1" && o.Type == "baron");
        var visitorBarons = objectives.Count(o => o.TeamTag == "GEN" && o.Type == "baron");

        var localTowers = elapsed switch
        {
            >= 1740 => 8,
            >= 1680 => 7,
            >= 1500 => 5,
            >= 1260 => 4,
            >= 840 => 2,
            >= 660 => 1,
            _ => 0
        };
        var visitorTowers = elapsed switch
        {
            >= 1660 => 3,
            >= 1020 => 2,
            >= 720 => 1,
            _ => 0
        };

        var localGold = CalculateGold(elapsed, basePerSecond: 28, localKills, localDragons, localBarons, localTowers);
        var visitorGold = CalculateGold(elapsed, basePerSecond: 26, visitorKills, visitorDragons, visitorBarons, visitorTowers);

        var local = new LiveTeamState(
            EquipoId: "seed:LOL_T1",
            Nombre: "T1",
            Tag: "T1",
            Pais: "KR",
            Kills: localKills,
            Torres: localTowers,
            Dragones: localDragons,
            Barones: localBarons,
            Oro: localGold,
            OroPorMinuto: GoldPerMinute(localGold, elapsed),
            VaGanando: localGold >= visitorGold);

        var visitor = new LiveTeamState(
            EquipoId: "seed:LOL_GENG",
            Nombre: "Gen.G",
            Tag: "GEN",
            Pais: "KR",
            Kills: visitorKills,
            Torres: visitorTowers,
            Dragones: visitorDragons,
            Barones: visitorBarons,
            Oro: visitorGold,
            OroPorMinuto: GoldPerMinute(visitorGold, elapsed),
            VaGanando: visitorGold > localGold);

        return new LiveMatchResponse(
            MatchId: "live-t1-geng-rift-2026",
            Estado: elapsed >= durationSeconds ? "FINALIZADA" : "EN_VIVO",
            DuracionSegundos: durationSeconds,
            SegundoActual: elapsed,
            Reloj: FormatClock(elapsed),
            Videojuego: "League of Legends",
            TorneoCodigo: "RIFT-LIVE26",
            TorneoNombre: "Rift Live Showcase 2026",
            Local: local,
            Visitante: visitor,
            Objetivos: objectives.Select(o => new LiveObjectiveEvent(o.Second, FormatClock(o.Second), o.Type, o.Name, o.TeamTag)).ToArray(),
            Timeline: moments.Select(m => new LiveTimelineEvent(
                m.Second,
                FormatClock(m.Second),
                m.TeamTag,
                m.Type,
                m.Text,
                m.LocalKills,
                m.VisitorKills)).ToArray(),
            Narrativa: NarrativeFor(elapsed, localGold - visitorGold));
    }

    private static int CalculateGold(int elapsed, int basePerSecond, int kills, int dragons, int barons, int towers)
        => 2500 + elapsed * basePerSecond + kills * 300 + dragons * 420 + barons * 1500 + towers * 650;

    private static int GoldPerMinute(int gold, int elapsed)
        => elapsed < 60 ? 0 : (int)Math.Round(gold / (elapsed / 60.0));

    private static string FormatClock(int seconds)
        => $"{seconds / 60:00}:{seconds % 60:00}";

    private static string NarrativeFor(int elapsed, int goldDiff)
    {
        if (elapsed < 240)
            return "Lineas estables, ambos equipos priorizan vision y control de oleadas.";
        if (elapsed < 720)
            return "T1 acelera el mapa con primer dragon y presion desde mid-jungle.";
        if (elapsed < 1200)
            return "Gen.G responde por side lanes, pero T1 conserva la iniciativa por objetivos.";
        if (elapsed < 1560)
            return $"T1 llega al mid game con {Math.Abs(goldDiff):N0} de diferencia de oro y setup de Baron.";
        if (elapsed < 1800)
            return "T1 usa Baron para romper la base y forzar la pelea final.";
        return "T1 cierra la simulacion con control de vision, Baron y pelea decisiva en base.";
    }

    private static readonly LiveMoment[] LiveMoments =
    [
        new(0, "SYSTEM", "start", "Minions en la grieta. Score inicial 0-0.", 0, 0),
        new(260, "T1", "kill", "Faker encuentra first blood en mid tras gank de jungle.", 1, 0),
        new(300, "T1", "objective", "T1 asegura el primer dragon al minuto 5.", 0, 0),
        new(445, "T1", "kill", "T1 castiga la rotacion de soporte y amplifica la ventaja.", 1, 0),
        new(520, "GEN", "kill", "Gen.G responde con pick sobre bot side.", 0, 1),
        new(780, "T1", "fight", "T1 gana la pelea por Heraldo y convierte placas en oro.", 2, 1),
        new(930, "GEN", "fight", "Gen.G encuentra dos bajas en side lane y reduce la diferencia.", 0, 2),
        new(1080, "T1", "objective", "Segundo dragon para T1; la condicion de alma queda abierta.", 0, 0),
        new(1260, "T1", "fight", "T1 gana pelea en rio y derriba la torre central.", 3, 1),
        new(1500, "T1", "baron", "T1 asegura Baron Nashor despues de forzar teleport de Gen.G.", 2, 1),
        new(1660, "GEN", "pick", "Gen.G consigue dos picks defensivos y retrasa el cierre.", 0, 2),
        new(1740, "T1", "fight", "T1 ejecuta la pelea final con engage frontal y control de carries.", 5, 1)
    ];

    private static readonly LiveObjective[] LiveObjectives =
    [
        new(300, "dragon", "Dragon infernal", "T1"),
        new(780, "herald", "Heraldo de la grieta", "T1"),
        new(1080, "dragon", "Dragon de montana", "T1"),
        new(1500, "baron", "Baron Nashor", "T1"),
        new(1680, "inhibitor", "Inhibidor central", "T1")
    ];

    private sealed record LiveMoment(int Second, string TeamTag, string Type, string Text, int LocalKills, int VisitorKills);
    private sealed record LiveObjective(int Second, string Type, string Name, string TeamTag);
}
