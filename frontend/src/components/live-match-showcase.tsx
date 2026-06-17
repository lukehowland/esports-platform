"use client";

import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Activity, Crown, Flame, Gauge, Radio, Shield, Swords, Timer, Trophy, Zap } from "lucide-react";
import { HudEyebrow, HudPanel } from "@/components/hud-panel";
import { Skeleton } from "@/components/ui/skeleton";
import { getPartidaEnVivoDestacada, type LiveMatchResponse, type LiveObjectiveEvent } from "@/lib/api/partidas";
import { cn } from "@/lib/utils";

const SPEEDS = [1, 2, 5, 15, 60];

const objectiveIcon = {
  dragon: Flame,
  herald: Shield,
  baron: Crown,
  inhibitor: Zap,
} as const;

const formatGold = (value: number) => `${(value / 1000).toFixed(1)}k`;

function objectiveLabel(objective: LiveObjectiveEvent) {
  const Icon = objectiveIcon[objective.tipo as keyof typeof objectiveIcon] ?? Trophy;
  return (
    <div key={`${objective.tipo}-${objective.segundo}`} className="hud-clip-sm border border-line bg-elevated/70 px-3 py-2">
      <div className="flex items-center gap-2">
        <Icon className={cn("h-4 w-4", objective.equipoTag === "T1" ? "text-lime" : "text-violet-bright")} />
        <div>
          <p className="text-xs font-display font-semibold text-foreground leading-none">{objective.nombre}</p>
          <p className="eyebrow mt-1">{objective.minuto} · {objective.equipoTag}</p>
        </div>
      </div>
    </div>
  );
}

function TeamPanel({ team, side, maxGold }: {
  team: LiveMatchResponse["local"];
  side: "left" | "right";
  maxGold: number;
}) {
  const goldWidth = Math.max(8, Math.round((team.oro / maxGold) * 100));

  return (
    <div className={cn("p-4 sm:p-6", side === "right" && "text-right")}>
      <div className={cn("flex items-start gap-3", side === "right" && "flex-row-reverse")}>
        <div className={cn(
          "grid h-12 w-12 place-items-center border font-display text-xl font-bold",
          team.vaGanando ? "border-lime/50 bg-lime/10 text-lime" : "border-line bg-elevated text-muted-foreground"
        )}>
          {team.tag}
        </div>
        <div className="min-w-0">
          <p className="eyebrow">{side === "left" ? "lado azul" : "lado rojo"} · {team.pais}</p>
          <h2 className="font-display text-3xl font-bold leading-none text-foreground">{team.nombre}</h2>
        </div>
      </div>

      <div className="mt-5 grid grid-cols-2 gap-2 text-sm sm:grid-cols-4">
        <Metric label="Kills" value={team.kills} active={team.vaGanando} />
        <Metric label="Torres" value={team.torres} />
        <Metric label="Dragones" value={team.dragones} />
        <Metric label="Baron" value={team.barones} />
      </div>

      <div className="mt-5">
        <div className="mb-2 flex items-center justify-between gap-3">
          <span className="eyebrow">oro total</span>
          <span className="font-mono text-sm font-bold text-gold">{formatGold(team.oro)}</span>
        </div>
        <div className="h-3 overflow-hidden border border-line bg-background">
          <div
            className={cn("h-full transition-all duration-700", team.vaGanando ? "bg-lime" : "bg-violet")}
            style={{ width: `${goldWidth}%` }}
          />
        </div>
        <p className="mt-2 text-xs text-muted-foreground">
          {team.oroPorMinuto > 0 ? `${team.oroPorMinuto.toLocaleString("es")} oro/min` : "fase inicial"}
        </p>
      </div>
    </div>
  );
}

function Metric({ label, value, active }: { label: string; value: number; active?: boolean }) {
  return (
    <div className={cn("border border-line bg-background/50 px-3 py-2", active && "border-lime/40 bg-lime/10")}>
      <p className="font-mono text-lg font-bold tabular-nums text-foreground">{value}</p>
      <p className="eyebrow mt-0.5">{label}</p>
    </div>
  );
}

export function LiveMatchShowcase() {
  const [elapsed, setElapsed] = useState(0);
  const [speedIndex, setSpeedIndex] = useState(0);
  const speed = SPEEDS[speedIndex];

  const { data, isLoading, error } = useQuery({
    queryKey: ["partidas", "en-vivo", "destacada", elapsed],
    queryFn: () => getPartidaEnVivoDestacada(elapsed),
    staleTime: 0,
  });

  useEffect(() => {
    const id = window.setInterval(() => {
      setElapsed((current) => Math.min(30 * 60, current + speed));
    }, 1000);

    return () => window.clearInterval(id);
  }, [speed]);

  const maxGold = useMemo(() => {
    if (!data) return 1;
    return Math.max(data.local.oro, data.visitante.oro, 1);
  }, [data]);

  const progress = data ? Math.round((data.segundoActual / data.duracionSegundos) * 100) : 0;

  const increaseSpeed = () => {
    if (elapsed >= 30 * 60) {
      setElapsed(0);
      setSpeedIndex(0);
      return;
    }

    setSpeedIndex((current) => Math.min(current + 1, SPEEDS.length - 1));
  };

  if (isLoading && !data) {
    return <Skeleton className="h-[520px] w-full max-w-6xl" />;
  }

  if (error || !data) {
    return (
      <HudPanel className="max-w-6xl p-6" accent="violet">
        <HudEyebrow>live feed no disponible</HudEyebrow>
        <p className="mt-2 text-sm text-muted-foreground">No se pudo cargar la partida destacada.</p>
      </HudPanel>
    );
  }

  const latestEvents = data.timeline.slice(-4).reverse();

  return (
    <section className="group relative mx-auto w-full max-w-6xl pt-4">
      <button
        type="button"
        onClick={increaseSpeed}
        title="Acelerar demo"
        aria-label="Acelerar simulacion de partida"
        className="absolute right-2 top-2 z-10 border border-line bg-background/90 px-2 py-1 font-mono text-[10px] text-muted-foreground opacity-0 transition-opacity hover:text-lime focus:opacity-100 focus:text-lime group-hover:opacity-60"
      >
        x{speed}
      </button>

      <div className="mb-4 flex flex-wrap items-center justify-center gap-3 text-center">
        <HudEyebrow className="text-lime">
          <span className="mr-2 inline-block h-1.5 w-1.5 rounded-full bg-lime animate-pulse" />
          EN VIVO · MATCH IN PROGRESS
        </HudEyebrow>
        <span className="eyebrow">{data.torneoCodigo} · {data.videojuego}</span>
      </div>

      <HudPanel className="overflow-hidden" accent="violet">
        <div className="border-b border-line bg-elevated/70 px-4 py-3">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <HudEyebrow>{data.torneoNombre}</HudEyebrow>
              <h1 className="mt-1 font-display text-3xl font-bold leading-none text-foreground sm:text-4xl">
                T1 vs Gen.G
              </h1>
            </div>
            <div className="flex items-center gap-3">
              <div className="hud-clip-sm border border-lime/40 bg-lime/10 px-4 py-2 text-lime">
                <div className="flex items-center gap-2">
                  <Timer className="h-4 w-4" />
                  <span className="font-mono text-2xl font-bold tabular-nums">{data.reloj}</span>
                </div>
              </div>
              <div className="hud-clip-sm border border-line bg-background px-3 py-2">
                <p className="eyebrow">BO1 · 30:00</p>
                <p className="font-mono text-xs text-muted-foreground">{data.estado === "FINALIZADA" ? "FINAL" : `${progress}%`}</p>
              </div>
            </div>
          </div>
          <div className="mt-3 h-1.5 overflow-hidden bg-background">
            <div className="h-full bg-lime transition-all duration-700" style={{ width: `${progress}%` }} />
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-[1fr_220px_1fr]">
          <TeamPanel team={data.local} side="left" maxGold={maxGold} />

          <div className="flex flex-col justify-center border-y border-line bg-background/60 px-4 py-6 text-center lg:border-x lg:border-y-0">
            <Swords className="mx-auto mb-3 h-6 w-6 text-violet-bright" />
            <div className="flex items-center justify-center gap-4">
              <span className="font-display text-6xl font-bold tabular-nums text-lime">{data.local.kills}</span>
              <span className="font-mono text-xl text-muted-foreground">-</span>
              <span className="font-display text-6xl font-bold tabular-nums text-foreground">{data.visitante.kills}</span>
            </div>
            <p className="eyebrow mt-2">kills</p>
            <div className="mt-5 grid grid-cols-2 gap-2">
              <div className="border border-line bg-elevated/60 px-2 py-2">
                <Gauge className="mx-auto mb-1 h-4 w-4 text-gold" />
                <p className="font-mono text-xs text-foreground">
                  +{Math.abs(data.local.oro - data.visitante.oro).toLocaleString("es")}
                </p>
                <p className="eyebrow mt-1">oro diff</p>
              </div>
              <div className="border border-line bg-elevated/60 px-2 py-2">
                <Radio className="mx-auto mb-1 h-4 w-4 text-lime" />
                <p className="font-mono text-xs text-foreground">{data.objetivos.length}</p>
                <p className="eyebrow mt-1">objetivos</p>
              </div>
            </div>
          </div>

          <TeamPanel team={data.visitante} side="right" maxGold={maxGold} />
        </div>

        <div className="grid gap-0 border-t border-line lg:grid-cols-[1fr_360px]">
          <div className="p-4 sm:p-5">
            <div className="mb-3 flex items-center gap-2">
              <Activity className="h-4 w-4 text-lime" />
              <HudEyebrow>narrativa del mapa</HudEyebrow>
            </div>
            <p className="text-sm leading-relaxed text-muted-foreground">{data.narrativa}</p>
            <div className="mt-4 grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
              {data.objetivos.length === 0 ? (
                <div className="hud-clip-sm border border-line bg-elevated/40 px-3 py-2">
                  <p className="text-xs text-muted-foreground">Sin objetivos mayores todavia.</p>
                  <p className="eyebrow mt-1">esperando minuto 05:00</p>
                </div>
              ) : data.objetivos.map(objectiveLabel)}
            </div>
          </div>

          <div className="border-t border-line bg-background/40 p-4 sm:p-5 lg:border-l lg:border-t-0">
            <HudEyebrow>ultimos eventos</HudEyebrow>
            <div className="mt-3 space-y-2">
              {latestEvents.map((event) => (
                <div key={`${event.segundo}-${event.texto}`} className="border border-line bg-elevated/50 px-3 py-2">
                  <div className="mb-1 flex items-center justify-between gap-2">
                    <span className={cn("font-mono text-xs font-bold", event.equipoTag === "T1" ? "text-lime" : "text-violet-bright")}>
                      {event.equipoTag}
                    </span>
                    <span className="font-mono text-xs text-muted-foreground">{event.minuto}</span>
                  </div>
                  <p className="text-xs leading-relaxed text-muted-foreground">{event.texto}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      </HudPanel>
    </section>
  );
}
