"use client";

import { useParams } from "next/navigation";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { User, Globe, Shield, History } from "lucide-react";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { getJugador, getMembresiasJugador } from "@/lib/api/equipos";
import { formatDate } from "@/lib/utils";

// Página pública de SOLO LECTURA: código, equipo actual y el historial de equipos
// del jugador (membresías). La gestión de roster (liberar/fichar/transferir) NO vive
// acá: el capitán la hace en /mi-equipo y el admin en /panel/equipos/[id].
export default function JugadorDetallePage() {
  const { id } = useParams<{ id: string }>();

  const { data: jugador, isLoading, error } = useQuery({
    queryKey: ["jugador", id],
    queryFn: () => getJugador(id),
  });

  const { data: membresias } = useQuery({
    queryKey: ["jugador", id, "membresias"],
    queryFn: () => getMembresiasJugador(id),
  });

  if (isLoading) return <Skeleton className="h-64 w-full max-w-3xl" />;
  if (error || !jugador) return <ErrorState error={error} />;

  const activa = membresias?.find((m) => m.activa) ?? null;
  const esAgenteLibre = !jugador.equipoId;

  return (
    <div className="space-y-6 max-w-3xl">
      <div>
        <Link href="/jugadores" className="eyebrow text-violet">← jugadores</Link>
        <div className="mt-2 flex flex-wrap items-center gap-3">
          <span className="hud-clip-sm border border-gold/40 bg-gold/10 text-gold font-mono text-sm px-2 py-1">
            {jugador.codigo}
          </span>
          <h1 className="text-3xl font-display font-bold tracking-wide text-foreground flex items-center gap-2">
            <User className="w-7 h-7 text-violet" /> {jugador.nickname}
          </h1>
        </div>
        <div className="mt-2 flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
          <span>{jugador.nombre}</span>
          <span className="flex items-center gap-1"><Globe className="w-3.5 h-3.5" /> {jugador.pais}</span>
          <Badge variant="muted">{jugador.rol}</Badge>
        </div>
      </div>

      {/* Equipo actual */}
      <HudPanel className="p-4">
        <HudEyebrow className="block mb-2">equipo actual</HudEyebrow>
        {esAgenteLibre ? (
          <p className="text-sm"><span className="hud-clip-sm border border-line bg-elevated text-muted-foreground text-xs px-2 py-0.5">AGENTE LIBRE</span> sin equipo activo</p>
        ) : (
          <Link href={`/equipos/${jugador.equipoId}`} className="inline-flex items-center gap-2 text-foreground hover:text-violet transition-colors">
            <Shield className="w-4 h-4 text-lime" />
            <span className="font-semibold">{activa?.nombreEquipo ?? "Equipo"}</span>
            {activa?.tag && <span className="font-mono text-xs text-muted-foreground">[{activa.tag}]</span>}
          </Link>
        )}
      </HudPanel>

      {/* Historial de equipos (membresías) */}
      <HudPanel>
        <div className="px-4 py-3 border-b border-line flex items-center gap-2">
          <History className="w-4 h-4 text-violet" />
          <HudEyebrow>historial de equipos</HudEyebrow>
        </div>
        {!membresias ? (
          <div className="p-4"><Skeleton className="h-16" /></div>
        ) : membresias.length === 0 ? (
          <EmptyState title="Sin historial" description="El jugador no tiene membresías registradas." />
        ) : (
          <div className="divide-y divide-line">
            {membresias.map((m) => (
              <div key={`${m.equipoId}-${m.fechaDesde}`} className="flex items-center justify-between px-4 py-3 gap-3">
                <div className="flex items-center gap-3 min-w-0">
                  <span className="hud-clip-sm border border-violet/30 bg-violet/10 text-violet font-mono text-xs px-2 py-0.5 shrink-0">
                    {m.tag}
                  </span>
                  <div className="min-w-0">
                    <p className="text-sm font-semibold text-foreground truncate">{m.nombreEquipo}</p>
                    <p className="eyebrow mt-0.5">{m.rol}</p>
                  </div>
                </div>
                <div className="text-right shrink-0">
                  {m.activa ? (
                    <span className="hud-clip-sm border border-lime/40 bg-lime/10 text-lime text-xs px-2 py-0.5">ACTUAL</span>
                  ) : (
                    <span className="eyebrow">hasta {m.fechaHasta ? formatDate(m.fechaHasta) : "—"}</span>
                  )}
                  <p className="eyebrow mt-1">desde {formatDate(m.fechaDesde)}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </HudPanel>
    </div>
  );
}
