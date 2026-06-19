"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { UserMinus, UserPlus, ArrowRightLeft, Loader2, Search } from "lucide-react";
import { toast } from "sonner";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter,
} from "@/components/ui/dialog";
import {
  getIntegrantesPorEquipo, getEquiposPorFecha, getJugadorPorNickname, getJugadorPorCodigo,
  liberarJugador, asignarJugador, type JugadorResponse,
} from "@/lib/api/equipos";
import { ApiError } from "@/lib/api/fetcher";

// Gestión de roster reutilizable (RF-03). Vive en la zona de cada rol:
//  - admin en /panel/equipos/[id] (esAdmin = true → puede transferir)
//  - capitán en /mi-equipo (esAdmin = false → libera y ficha a su propio equipo)
// El backend valida el ownership; la UI solo muestra lo que corresponde.
export function RosterManager({ equipoId, equipoNombre, esAdmin }: {
  equipoId: string;
  equipoNombre: string;
  esAdmin: boolean;
}) {
  const qc = useQueryClient();

  const { data: integrantes, isLoading } = useQuery({
    queryKey: ["integrantes", equipoId],
    queryFn: () => getIntegrantesPorEquipo(equipoId),
  });

  const refrescar = () => {
    qc.invalidateQueries({ queryKey: ["integrantes"] });
    qc.invalidateQueries({ queryKey: ["jugador"] });
    qc.invalidateQueries({ queryKey: ["equipo"] });
  };

  const liberar = useMutation({
    mutationFn: (jugadorId: string) => liberarJugador(jugadorId),
    onSuccess: () => { toast.success("Jugador liberado — agente libre"); refrescar(); },
    onError: (e) => toast.error(e instanceof ApiError ? e.detail : "No se pudo liberar"),
  });

  return (
    <div className="space-y-4">
      <HudPanel>
        <div className="px-4 py-3 border-b border-line">
          <HudEyebrow>roster activo</HudEyebrow>
        </div>
        {isLoading ? (
          <div className="p-4 space-y-2">{[...Array(3)].map((_, i) => <Skeleton key={i} className="h-12" />)}</div>
        ) : !integrantes || integrantes.length === 0 ? (
          <EmptyState title="Sin jugadores" description="El equipo no tiene integrantes activos." />
        ) : (
          <div className="divide-y divide-line">
            {integrantes.map((j) => (
              <div key={j.jugadorId} className="flex items-center justify-between px-4 py-3 gap-3">
                <div className="flex items-center gap-3 min-w-0">
                  <span className="hud-clip-sm border border-gold/30 bg-gold/10 text-gold text-[10px] font-mono px-1.5 py-0.5 shrink-0">{j.codigo}</span>
                  <div className="min-w-0">
                    <p className="text-sm font-semibold text-foreground truncate">{j.nickname}</p>
                    <p className="eyebrow mt-0.5">{j.nombre} · {j.rol}</p>
                  </div>
                </div>
                <div className="flex items-center gap-1 shrink-0">
                  {esAdmin && <TransferirDialog jugador={j} equipoActualId={equipoId} onDone={refrescar} />}
                  <Button size="sm" variant="outline" disabled={liberar.isPending} onClick={() => liberar.mutate(j.jugadorId)}>
                    <UserMinus className="w-3.5 h-3.5" /> Liberar
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}
      </HudPanel>

      <FicharAgenteLibre equipoId={equipoId} equipoNombre={equipoNombre} onDone={refrescar} />
    </div>
  );
}

// Buscar un agente libre por código (J-001) o nickname y ficharlo a este equipo.
function FicharAgenteLibre({ equipoId, equipoNombre, onDone }: {
  equipoId: string; equipoNombre: string; onDone: () => void;
}) {
  const [input, setInput] = useState("");
  const [query, setQuery] = useState("");

  const esCodigo = /^J-/i.test(query.trim());
  const { data: encontrado, isFetching, error } = useQuery({
    queryKey: ["jugador", "buscar", query],
    queryFn: () => (esCodigo
      ? getJugadorPorCodigo(query.trim().toUpperCase())
      : getJugadorPorNickname(query.trim())),
    enabled: !!query,
    retry: false,
  });

  const fichar = useMutation({
    mutationFn: (jugadorId: string) => asignarJugador(jugadorId, { equipoDestinoId: equipoId }),
    onSuccess: () => { toast.success("Jugador fichado"); setInput(""); setQuery(""); onDone(); },
    onError: (e) => toast.error(e instanceof ApiError ? e.detail : "No se pudo fichar"),
  });

  const noEncontrado = error instanceof ApiError && error.status === 404;
  const libre = encontrado && !encontrado.equipoId;

  return (
    <HudPanel className="p-4 space-y-3">
      <HudEyebrow className="block">fichar agente libre a {equipoNombre}</HudEyebrow>
      <div className="flex gap-2 max-w-md">
        <Input
          placeholder="Código (J-201) o nickname…"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && setQuery(input.trim())}
        />
        <Button variant="outline" size="icon" onClick={() => setQuery(input.trim())}><Search className="w-4 h-4" /></Button>
      </div>

      {query && (
        isFetching ? <Skeleton className="h-12 max-w-md" /> :
        noEncontrado ? <p className="text-sm text-muted-foreground">No se encontró <span className="font-mono">{query}</span>.</p> :
        encontrado ? (
          <div className="flex items-center justify-between gap-3 border border-line bg-elevated/50 px-3 py-2 max-w-md">
            <div className="min-w-0">
              <p className="text-sm font-semibold text-foreground truncate">
                <span className="font-mono text-gold text-xs mr-2">{encontrado.codigo}</span>{encontrado.nickname}
              </p>
              <p className="eyebrow mt-0.5">{libre ? "agente libre" : "ya tiene equipo activo"}</p>
            </div>
            <Button size="sm" disabled={!libre || fichar.isPending} onClick={() => fichar.mutate(encontrado.jugadorId)}>
              {fichar.isPending ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <UserPlus className="w-3.5 h-3.5" />} Fichar
            </Button>
          </div>
        ) : null
      )}
      {encontrado && !libre && (
        <p className="text-xs text-muted-foreground max-w-md">
          Para fichar a este jugador, su equipo actual debe liberarlo primero.
        </p>
      )}
    </HudPanel>
  );
}

// Traspaso atómico (solo admin): elige equipo destino y transfiere.
function TransferirDialog({ jugador, equipoActualId, onDone }: {
  jugador: JugadorResponse; equipoActualId: string; onDone: () => void;
}) {
  const [open, setOpen] = useState(false);
  const [destino, setDestino] = useState("");

  const { data: equipos } = useQuery({
    queryKey: ["equipos", "por-fecha"],
    queryFn: getEquiposPorFecha,
    enabled: open,
  });

  const transferir = useMutation({
    mutationFn: () => asignarJugador(jugador.jugadorId, { equipoDestinoId: destino }),
    onSuccess: () => { toast.success("Jugador transferido"); setOpen(false); setDestino(""); onDone(); },
    onError: (e) => toast.error(e instanceof ApiError ? e.detail : "No se pudo transferir"),
  });

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <Button size="sm" variant="ghost" onClick={() => setOpen(true)} aria-label={`Transferir ${jugador.nickname}`}>
        <ArrowRightLeft className="w-3.5 h-3.5 text-violet" />
      </Button>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Transferir {jugador.nickname}</DialogTitle>
          <DialogDescription>Traspaso atómico a otro equipo (cierra su membresía actual y abre la nueva).</DialogDescription>
        </DialogHeader>
        <div className="space-y-1.5">
          <Label className="eyebrow">Equipo destino</Label>
          <Select value={destino} onValueChange={setDestino}>
            <SelectTrigger><SelectValue placeholder="Elegí un equipo…" /></SelectTrigger>
            <SelectContent>
              {(equipos ?? []).filter((e) => e.equipoId !== equipoActualId).map((e) => (
                <SelectItem key={e.equipoId} value={e.equipoId}>[{e.tag}] {e.nombre}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => setOpen(false)}>Cancelar</Button>
          <Button disabled={!destino || transferir.isPending} onClick={() => transferir.mutate()}>
            {transferir.isPending && <Loader2 className="w-4 h-4 animate-spin" />} Transferir
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
