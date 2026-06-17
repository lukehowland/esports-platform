import { cn } from "@/lib/utils";

interface ScoreboardProps {
  nombreLocal: string;
  nombreVisitante: string;
  resultado: string;     // "2-1", "13-7", etc.
  ganador?: string;      // nombre del ganador (opcional)
  nombreTorneo?: string;
  fecha?: string;
  className?: string;
}

/**
 * Scoreboard — elemento firma visual del sitio.
 * Panel de resultado de partida estilo overlay de transmisión eSports:
 * corte diagonal + tratamiento VICTORIA/DERROTA.
 */
export function Scoreboard({ nombreLocal, nombreVisitante, resultado, ganador, nombreTorneo, fecha, className }: ScoreboardProps) {
  const partes = resultado.split("-");
  const scoreLocal     = partes[0] ?? "0";
  const scoreVisitante = partes[1] ?? "0";
  const localGana    = ganador === nombreLocal;
  const visitanteGana = ganador === nombreVisitante;

  return (
    <div className={cn("hud-clip border border-line bg-panel overflow-hidden", className)}>
      {/* Header de contexto */}
      {(nombreTorneo || fecha) && (
        <div className="flex items-center justify-between px-4 pt-3 pb-2 border-b border-line">
          {nombreTorneo && <span className="eyebrow">{nombreTorneo}</span>}
          {fecha && <span className="eyebrow">{fecha}</span>}
        </div>
      )}

      {/* Scoreboard principal */}
      <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-0">
        {/* Local */}
        <div className={cn(
          "px-4 py-4 text-center transition-colors",
          localGana && "bg-violet/5"
        )}>
          <p className={cn(
            "text-sm font-semibold truncate",
            localGana ? "text-foreground" : "text-muted-foreground"
          )}>
            {nombreLocal}
          </p>
          {localGana && (
            <span className="inline-block mt-1 text-[10px] font-mono font-bold uppercase tracking-widest text-lime">
              ▰▰ victoria
            </span>
          )}
        </div>

        {/* Marcador central */}
        <div className="px-6 py-4 text-center border-x border-line bg-elevated">
          <div className="flex items-baseline gap-3 justify-center">
            <span className={cn(
              "text-4xl font-display font-bold tabular-nums leading-none",
              localGana ? "text-foreground" : "text-muted-foreground"
            )}>
              {scoreLocal}
            </span>
            <span className="text-lg text-line font-mono">—</span>
            <span className={cn(
              "text-4xl font-display font-bold tabular-nums leading-none",
              visitanteGana ? "text-foreground" : "text-muted-foreground"
            )}>
              {scoreVisitante}
            </span>
          </div>
        </div>

        {/* Visitante */}
        <div className={cn(
          "px-4 py-4 text-center transition-colors",
          visitanteGana && "bg-violet/5"
        )}>
          <p className={cn(
            "text-sm font-semibold truncate",
            visitanteGana ? "text-foreground" : "text-muted-foreground"
          )}>
            {nombreVisitante}
          </p>
          {visitanteGana && (
            <span className="inline-block mt-1 text-[10px] font-mono font-bold uppercase tracking-widest text-lime">
              ▰▰ victoria
            </span>
          )}
        </div>
      </div>
    </div>
  );
}

/** Versión compacta para listas de partidas */
export function ScoreboardRow({ nombreLocal, nombreVisitante, resultado, ganador, className }: Omit<ScoreboardProps, "nombreTorneo" | "fecha"> & { className?: string }) {
  const partes = resultado.split("-");
  const scoreLocal     = partes[0] ?? "?";
  const scoreVisitante = partes[1] ?? "?";

  return (
    <div className={cn("flex items-center gap-2 text-sm", className)}>
      <span className={cn("flex-1 text-right font-semibold truncate", ganador === nombreLocal ? "text-foreground" : "text-muted-foreground")}>
        {nombreLocal}
      </span>
      <span className="font-display font-bold text-base tabular-nums shrink-0 px-2 py-0.5 bg-elevated border border-line rounded text-foreground">
        {scoreLocal} — {scoreVisitante}
      </span>
      <span className={cn("flex-1 font-semibold truncate", ganador === nombreVisitante ? "text-foreground" : "text-muted-foreground")}>
        {nombreVisitante}
      </span>
    </div>
  );
}
