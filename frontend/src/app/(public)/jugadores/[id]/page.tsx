"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { useQuery, useQueries, useMutation, useQueryClient } from "@tanstack/react-query";
import { User, Globe, Shield, ArrowRightLeft, UserMinus, UserPlus, Loader2, History } from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/lib/auth/context";
import { isAdmin, isCapitan } from "@/lib/auth/types";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  getJugador, getMembresiasJugador, getEquiposPorFecha,
  liberarJugador, asignarJugador,
} from "@/lib/api/equipos";
import { ApiError } from "@/lib/api/fetcher";
import { formatDate } from "@/lib/utils";

export default function JugadorDetallePage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const { identidad } = useAuth();
  const admin = isAdmin(identidad);
  const capitan = isCapitan(identidad) ? identidad : null;

  const { data: jugador, isLoading, error } = useQuery({
    queryKey: ["jugador", id],
    queryFn: () => getJugador(id),
  });

  const { data: membresias } = useQuery({
    queryKey: ["jugador", id, "membresias"],
    queryFn: () => getMembresiasJugador(id),
  });

  const { data: equipos } = useQuery({
    queryKey: ["equipos", "por-fecha"],
    queryFn: getEquiposPorFecha,
  });

  const [destino, setDestino] = useState("");

  const refrescar = () => {
    qc.invalidateQueries({ queryKey: ["jugador", id] });
    qc.invalidateQueries({ queryKey: ["equipos"] });
    qc.invalidateQueries({ queryKey: ["equipo"] });
    qc.invalidateQueries({ queryKey: ["integrantes"] });
  };

  const liberar = useMutation({
    mutationFn: () => liberarJugador(id),
    onSuccess: () => { toast.success("Jugador liberado — ahora es agente libre"); refrescar(); },
    onError: (e) => toast.error(e instanceof ApiError ? e.detail : "No se pudo liberar"),
  });

  const asignar = useMutation({
    mutationFn: (equipoDestinoId: string) => asignarJugador(id, { equipoDestinoId }),
    onSuccess: () => { toast.success("Jugador fichado"); setDestino(""); refrescar(); },
    onError: (e) => toast.error(e instanceof ApiError ? e.detail : "No se pudo fichar"),
  });

  if (isLoading) return <Skeleton className="h-64 w-full max-w-3xl" />;
  if (error || !jugador) return <ErrorState error={error} />;

  const activa = membresias?.find((m) => m.activa) ?? null;
  const esAgenteLibre = !jugador.equipoId;
  const miEquipo = capitan?.equipoId ?? null;
  const esMiJugador = !!miEquipo && jugador.equipoId === miEquipo;

  // Equipos a los que se puede fichar (excluye el equipo activo actual).
  const equiposDestino = (equipos ?? []).filter((e) => e.equipoId !== jugador.equipoId);

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

      {/* Acciones (admin / capitán) */}
      {(admin || capitan) && (
        <HudPanel className="p-4 space-y-3">
          <HudEyebrow className="block">gestión de roster (RF-03)</HudEyebrow>

          {/* Liberar: admin siempre; capitán solo su jugador */}
          {!esAgenteLibre && (admin || esMiJugador) && (
            <Button variant="outline" disabled={liberar.isPending} onClick={() => liberar.mutate()}>
              {liberar.isPending ? <Loader2 className="w-4 h-4 animate-spin" /> : <UserMinus className="w-4 h-4" />}
              Liberar (dar de baja)
            </Button>
          )}

          {/* Fichar agente libre: admin elige equipo; capitán ficha a su equipo */}
          {esAgenteLibre && capitan && (
            <Button disabled={asignar.isPending} onClick={() => asignar.mutate(miEquipo!)}>
              {asignar.isPending ? <Loader2 className="w-4 h-4 animate-spin" /> : <UserPlus className="w-4 h-4" />}
              Fichar a mi equipo
            </Button>
          )}

          {/* Admin: fichar (agente libre) o transferir (con equipo) a cualquier equipo */}
          {admin && (
            <div className="flex flex-wrap items-end gap-2">
              <div className="space-y-1.5 min-w-[14rem]">
                <span className="eyebrow">{esAgenteLibre ? "fichar a" : "transferir a"}</span>
                <Select value={destino} onValueChange={setDestino}>
                  <SelectTrigger><SelectValue placeholder="Elegí un equipo…" /></SelectTrigger>
                  <SelectContent>
                    {equiposDestino.map((e) => (
                      <SelectItem key={e.equipoId} value={e.equipoId}>[{e.tag}] {e.nombre}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <Button disabled={!destino || asignar.isPending} onClick={() => asignar.mutate(destino)}>
                {asignar.isPending ? <Loader2 className="w-4 h-4 animate-spin" /> : <ArrowRightLeft className="w-4 h-4" />}
                {esAgenteLibre ? "Fichar" : "Transferir"}
              </Button>
            </div>
          )}

          {capitan && !esAgenteLibre && !esMiJugador && (
            <p className="text-xs text-muted-foreground">
              El jugador tiene equipo activo en otro club. Para ficharlo, su equipo debe liberarlo primero.
            </p>
          )}
        </HudPanel>
      )}

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
