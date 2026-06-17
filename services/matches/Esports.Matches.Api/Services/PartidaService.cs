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

        // T1 arranca por detras en mapa y, con la remontada, supera a Gen.G en torres.
        var localTowers = elapsed switch
        {
            >= 1740 => 11,
            >= 1620 => 10,
            >= 1470 => 8,
            >= 1230 => 6,
            >= 1020 => 4,
            >= 760 => 2,
            _ => 0
        };
        var visitorTowers = elapsed switch
        {
            >= 1740 => 4,
            >= 1200 => 3,
            >= 760 => 2,
            >= 400 => 1,
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
        if (elapsed < 300)
            return "Gen.G sale agresivo y domina los primeros duelos: el lado rojo manda 0-2 en kills.";
        if (elapsed < 700)
            return "T1 responde con macro, asegura el primer dragon al minuto 5 y estabiliza las lineas.";
        if (elapsed < 1020)
            return "Intercambio parejo por el mapa; T1 empata con Heraldo y objetivos e inicia la remontada.";
        if (elapsed < 1410)
            return $"T1 completa la remontada y toma la delantera con {Math.Abs(goldDiff):N0} de oro a favor.";
        if (elapsed < 1740)
            return "T1 controla alma de dragon y Baron; presiona las calles laterales de Gen.G.";
        if (elapsed < 1800)
            return "T1 abre la base con segundo Baron e inhibidores rumbo al nexo de Gen.G.";
        return "T1 sentencia la simulacion: remontada completada y nexo de Gen.G derribado.";
    }

    private static readonly LiveMoment[] LiveMoments =
    [
        new(0, "SYSTEM", "start", "Minions en la grieta. Score inicial 0-0.", 0, 0),
        // Fase temprana: Gen.G sale agresivo y se va arriba 0-2 antes del minuto 5.
        new(95, "GEN", "kill", "Gen.G abre el marcador con first blood en bot lane.", 0, 1),
        new(215, "GEN", "kill", "Segundo gank de Gen.G por top; el lado rojo manda 0-2.", 0, 1),
        new(300, "T1", "objective", "T1 responde con macro y asegura el primer dragon al minuto 5.", 0, 0),
        new(385, "GEN", "fight", "Gen.G gana la pelea por el rio y estira la ventaja temprana.", 0, 2),
        new(470, "T1", "kill", "T1 castiga la sobre-extension de Gen.G y consigue dos picks.", 2, 0),
        new(560, "GEN", "objective", "Gen.G roba el segundo dragon y mantiene el control de oro.", 0, 0),
        new(615, "T1", "fight", "Intercambio parejo en mid; T1 encuentra una baja mas.", 2, 1),
        new(700, "T1", "objective", "T1 toma el Heraldo de la grieta y abre presion en top.", 0, 0),
        new(760, "T1", "fight", "T1 convierte el Heraldo en torre y empieza la remontada.", 3, 1),
        new(880, "GEN", "fight", "Gen.G responde por side lanes y empata la pelea de kills.", 1, 2),
        new(960, "T1", "objective", "Segundo dragon para T1; la condicion de alma queda abierta.", 0, 0),
        new(1020, "T1", "fight", "T1 gana el teamfight de mid game y toma la delantera.", 3, 1),
        new(1140, "GEN", "pick", "Gen.G logra un pick defensivo y retrasa el cierre.", 1, 2),
        new(1230, "T1", "fight", "T1 domina la pelea por el medio y amplia la ventaja en kills.", 4, 1),
        new(1300, "T1", "objective", "T1 cierra el alma del dragon y escala su composicion.", 0, 0),
        new(1410, "T1", "baron", "T1 asegura Baron Nashor tras forzar el teleport de Gen.G.", 0, 0),
        new(1470, "T1", "fight", "Con Baron en mano T1 asedia y consigue cuatro bajas.", 4, 2),
        new(1560, "GEN", "fight", "Gen.G gana una pelea defensiva y aguanta en su base.", 2, 3),
        new(1620, "T1", "objective", "T1 derriba el inhibidor central y libera super-minions.", 0, 0),
        new(1660, "T1", "objective", "T1 captura el Dragon anciano antes del asalto final.", 0, 0),
        new(1700, "T1", "baron", "Segundo Baron para T1; el cierre es cuestion de tiempo.", 0, 0),
        new(1740, "T1", "fight", "T1 ejecuta la pelea final con engage frontal sobre el nexo.", 6, 1)
    ];

    private static readonly LiveObjective[] LiveObjectives =
    [
        new(300, "dragon", "Dragon infernal", "T1"),
        new(560, "dragon", "Dragon de oceano", "GEN"),
        new(700, "herald", "Heraldo de la grieta", "T1"),
        new(960, "dragon", "Dragon de montana", "T1"),
        new(1300, "dragon", "Alma del dragon", "T1"),
        new(1410, "baron", "Baron Nashor", "T1"),
        new(1620, "inhibitor", "Inhibidor central", "T1"),
        new(1660, "dragon", "Dragon anciano", "T1"),
        new(1700, "baron", "Segundo Baron Nashor", "T1")
    ];

    private sealed record LiveMoment(int Second, string TeamTag, string Type, string Text, int LocalKills, int VisitorKills);
    private sealed record LiveObjective(int Second, string Type, string Name, string TeamTag);
}
