"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Users, Flag, ArrowLeft, Pencil, Trash2, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { RequireRole } from "@/lib/auth/require-role";
import { HudPanel } from "@/components/hud-panel";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter,
} from "@/components/ui/dialog";
import { ErrorState } from "@/components/error-state";
import { RosterManager } from "@/components/roster-manager";
import { getEquipoPorId, editarEquipo, eliminarEquipo, type EquipoResponse } from "@/lib/api/equipos";
import { ApiError } from "@/lib/api/fetcher";

export default function PanelEquipoDetallePage() {
  return (
    <RequireRole roles={["admin"]}>
      <EquipoDetalleContent />
    </RequireRole>
  );
}

function EquipoDetalleContent() {
  const { id } = useParams<{ id: string }>();

  const { data: equipo, isLoading, error } = useQuery({
    queryKey: ["equipo", id],
    queryFn: () => getEquipoPorId(id),
  });

  if (isLoading) return <Skeleton className="h-64 w-full max-w-3xl" />;
  if (error || !equipo) return <ErrorState error={error} />;

  return (
    <div className="space-y-6 max-w-3xl">
      <div className="flex flex-col items-start justify-between gap-3 sm:flex-row">
        <div>
          <Link href="/panel/equipos" className="eyebrow text-violet flex items-center gap-1">
            <ArrowLeft className="w-3.5 h-3.5" /> equipos
          </Link>
          <h1 className="mt-2 flex flex-wrap items-center gap-3 text-3xl font-display font-bold tracking-wide text-foreground">
            <span className="hud-clip-sm border border-violet/40 bg-violet/10 text-violet px-3 py-1 text-xl font-mono">
              {equipo.tag}
            </span>
            {equipo.nombre}
          </h1>
          <p className="eyebrow mt-2 flex items-center gap-1"><Flag className="w-3 h-3" /> {equipo.pais}</p>
        </div>
        <EquipoActions equipo={equipo} />
      </div>

      <div className="flex items-center gap-2">
        <Users className="w-4 h-4 text-violet" />
        <p className="eyebrow">gestión de roster (RF-03)</p>
      </div>

      <RosterManager equipoId={equipo.equipoId} equipoNombre={equipo.nombre} esAdmin />
    </div>
  );
}

// RF-02: editar / eliminar equipo (admin; bloqueado si tiene roster).
function EquipoActions({ equipo }: { equipo: EquipoResponse }) {
  const router = useRouter();
  const qc = useQueryClient();
  const [editOpen, setEditOpen] = useState(false);
  const [delOpen, setDelOpen] = useState(false);
  const [nombre, setNombre] = useState(equipo.nombre);
  const [tag, setTag] = useState(equipo.tag);
  const [pais, setPais] = useState(equipo.pais);

  const abrirEdicion = () => {
    setNombre(equipo.nombre);
    setTag(equipo.tag);
    setPais(equipo.pais);
    setEditOpen(true);
  };

  const editar = useMutation({
    mutationFn: () => editarEquipo(equipo.equipoId, { nombre: nombre.trim(), tag: tag.trim(), pais: pais.trim() }),
    onSuccess: () => {
      toast.success("Equipo actualizado");
      qc.invalidateQueries({ queryKey: ["equipo", equipo.equipoId] });
      qc.invalidateQueries({ queryKey: ["equipos"] });
      setEditOpen(false);
    },
    onError: (e) => toast.error(e instanceof ApiError ? e.detail : "No se pudo editar"),
  });

  const eliminar = useMutation({
    mutationFn: () => eliminarEquipo(equipo.equipoId),
    onSuccess: () => {
      toast.success("Equipo eliminado");
      qc.invalidateQueries({ queryKey: ["equipos"] });
      router.push("/panel/equipos");
    },
    onError: (e) => toast.error(e instanceof ApiError ? e.detail : "No se pudo eliminar"),
  });

  return (
    <div className="flex items-center gap-1 shrink-0">
      <Button size="sm" variant="outline" onClick={abrirEdicion}><Pencil className="w-3.5 h-3.5" /> Editar</Button>
      <Button size="sm" variant="outline" onClick={() => setDelOpen(true)}><Trash2 className="w-3.5 h-3.5 text-destructive" /> Eliminar</Button>

      <Dialog open={editOpen} onOpenChange={setEditOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Editar equipo</DialogTitle>
            <DialogDescription>No se puede editar un equipo con roster. Liberá sus jugadores primero.</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="space-y-1.5">
              <Label className="eyebrow">Nombre</Label>
              <Input value={nombre} onChange={(e) => setNombre(e.target.value)} />
            </div>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label className="eyebrow">Tag</Label>
                <Input value={tag} onChange={(e) => setTag(e.target.value)} />
              </div>
              <div className="space-y-1.5">
                <Label className="eyebrow">País (ISO-2)</Label>
                <Input value={pais} onChange={(e) => setPais(e.target.value)} />
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setEditOpen(false)}>Cancelar</Button>
            <Button disabled={editar.isPending} onClick={() => editar.mutate()}>
              {editar.isPending && <Loader2 className="w-4 h-4 animate-spin" />} Guardar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={delOpen} onOpenChange={setDelOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Eliminar equipo</DialogTitle>
            <DialogDescription>
              Vas a eliminar <span className="font-semibold text-foreground">{equipo.nombre}</span>.
              No se puede eliminar un equipo con roster.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDelOpen(false)}>Cancelar</Button>
            <Button variant="destructive" disabled={eliminar.isPending} onClick={() => eliminar.mutate()}>
              {eliminar.isPending && <Loader2 className="w-4 h-4 animate-spin" />} Eliminar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
