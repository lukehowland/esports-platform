"use client";

import { useState } from "react";
import { useQueries, useQuery } from "@tanstack/react-query";
import { Gamepad2, ChevronDown, ChevronRight } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { getVideojuegosPorGenero, getTorneosPorVideojuego } from "@/lib/api/torneos";
import { formatDate } from "@/lib/utils";

const GENEROS = ["MOBA", "FPS", "BATTLE_ROYALE", "RTS", "FIGHTING", "SPORTS", "RPG"];
const TODOS = "TODOS";

function TorneosPorVideojuego({ videojuegoId, nombre }: { videojuegoId: string; nombre: string }) {
  const [expanded, setExpanded] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["torneos", "por-videojuego", videojuegoId],
    queryFn: () => getTorneosPorVideojuego(videojuegoId),
    enabled: expanded,
  });

  return (
    <div>
      <button
        onClick={() => setExpanded(!expanded)}
        className="flex items-center gap-1.5 text-xs text-primary hover:underline mt-2"
      >
        {expanded ? <ChevronDown className="h-3 w-3" /> : <ChevronRight className="h-3 w-3" />}
        Ver torneos de {nombre}
      </button>
      {expanded && (
        <div className="mt-2 pl-2 border-l border-border space-y-1">
          {isLoading ? <Skeleton className="h-12" /> :
            data?.length === 0 ? <p className="text-xs text-muted-foreground">Sin torneos.</p> :
            data?.map((t) => (
              <div key={t.torneoId} className="flex items-center justify-between py-1">
                <span className="text-xs text-foreground">{t.nombreTorneo}</span>
                <div className="flex items-center gap-2">
                  <span className="text-xs text-muted-foreground">{t.nombreOrganizador}</span>
                  <span className="text-xs text-muted-foreground">{formatDate(t.fechaInicio)}</span>
                </div>
              </div>
            ))
          }
        </div>
      )}
    </div>
  );
}

export default function VideojuegosPage() {
  const [generoSeleccionado, setGeneroSeleccionado] = useState(TODOS);

  // Query-first: no hay "listar todos los videojuegos". Se hace fan-out por los 7
  // géneros (Q8), se etiqueta cada juego con su género y se filtra en memoria. Así
  // "TODOS" muestra el catálogo completo y los chips filtran sin pedir de nuevo.
  const { juegos, cargando, error } = useQueries({
    queries: GENEROS.map((g) => ({
      queryKey: ["videojuegos", g],
      queryFn: () => getVideojuegosPorGenero(g),
    })),
    combine: (results) => {
      const seen = new Set<string>();
      const juegos: { videojuegoId: string; nombre: string; genero: string; plataforma: string }[] = [];
      results.forEach((r, i) => {
        for (const vg of r.data ?? []) {
          if (!seen.has(vg.videojuegoId)) {
            seen.add(vg.videojuegoId);
            juegos.push({ videojuegoId: vg.videojuegoId, nombre: vg.nombre, genero: GENEROS[i], plataforma: vg.plataforma });
          }
        }
      });
      return {
        juegos,
        cargando: results.some((r) => r.isLoading),
        error: results.find((r) => r.error)?.error ?? null,
      };
    },
  });

  const visibles =
    generoSeleccionado === TODOS ? juegos : juegos.filter((j) => j.genero === generoSeleccionado);

  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ catálogo</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <Gamepad2 className="w-7 h-7 text-violet" /> Videojuegos
        </h1>
      </div>

      {/* Selector de género */}
      <div className="flex flex-wrap gap-1.5">
        {[TODOS, ...GENEROS].map((g) => (
          <button
            key={g}
            onClick={() => setGeneroSeleccionado(g)}
            className={`eyebrow px-2 py-0.5 rounded hud-clip-sm border text-xs transition-colors ${
              generoSeleccionado === g
                ? "border-violet/60 bg-violet/15 text-violet"
                : "border-line bg-elevated text-muted-foreground hover:text-foreground hover:border-violet/30"
            }`}
          >
            {g}
          </button>
        ))}
      </div>

      <HudPanel>
        <div className="px-4 py-3 border-b border-line">
          <HudEyebrow>{generoSeleccionado} — {cargando ? "…" : visibles.length} juegos</HudEyebrow>
        </div>
        {cargando ? (
          <div className="p-4 space-y-2">{[...Array(4)].map((_, i) => <Skeleton key={i} className="h-16" />)}</div>
        ) : error ? <ErrorState error={error} /> :
        visibles.length === 0 ? (
          <EmptyState title={`Sin videojuegos en ${generoSeleccionado}`} description="No hay videojuegos registrados en este género." />
        ) : (
          <div className="divide-y divide-line">
            {visibles.map((vg) => (
              <div key={vg.videojuegoId} className="px-4 py-3">
                <div className="flex items-center gap-2 mb-1">
                  <Gamepad2 className="h-4 w-4 text-violet" />
                  <p className="font-semibold text-foreground">{vg.nombre}</p>
                  <span className="hud-clip-sm border border-lime/30 bg-lime/10 text-lime font-mono text-xs px-2 py-0.5 ml-auto">
                    {vg.plataforma}
                  </span>
                  <span className="hud-clip-sm border border-violet/30 bg-violet/10 text-violet font-mono text-xs px-2 py-0.5">
                    {vg.genero}
                  </span>
                </div>
                <TorneosPorVideojuego videojuegoId={vg.videojuegoId} nombre={vg.nombre} />
              </div>
            ))}
          </div>
        )}
      </HudPanel>
    </div>
  );
}
