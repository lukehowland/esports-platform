"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Building2, Loader2, Plus, Pencil, Trash2, ShieldAlert } from "lucide-react";
import { toast } from "sonner";
import { RequireRole } from "@/lib/auth/require-role";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { StatTile } from "@/components/stat-tile";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter,
} from "@/components/ui/dialog";
import {
  getOrganizadores, crearOrganizador, editarOrganizador, eliminarOrganizador,
  type OrganizadorResponse,
} from "@/lib/api/torneos";
import { ApiError } from "@/lib/api/fetcher";

const schema = z.object({ nombre: z.string().min(1, "Requerido").max(120) });
type Form = z.infer<typeof schema>;

export default function PanelOrganizadoresPage() {
  return (
    <RequireRole roles={["admin"]}>
      <OrganizadoresContent />
    </RequireRole>
  );
}

function OrganizadoresContent() {
  const qc = useQueryClient();
  const [serverError, setServerError] = useState<string | null>(null);
  const [aEditar, setAEditar] = useState<OrganizadorResponse | null>(null);
  const [aEliminar, setAEliminar] = useState<OrganizadorResponse | null>(null);

  const { data: orgs, isLoading } = useQuery({
    queryKey: ["organizadores"],
    queryFn: getOrganizadores,
  });

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<Form>({
    resolver: zodResolver(schema),
  });

  const refrescar = () => qc.invalidateQueries({ queryKey: ["organizadores"] });

  const crear = useMutation({
    mutationFn: (d: Form) => crearOrganizador({ nombre: d.nombre }),
    onSuccess: () => {
      toast.success("Organizador creado");
      reset();
      setServerError(null);
      refrescar();
    },
    onError: (err) => {
      setServerError(err instanceof ApiError ? err.detail : "Error al crear organizador");
    },
  });

  const eliminar = useMutation({
    mutationFn: (id: string) => eliminarOrganizador(id),
    onSuccess: () => {
      toast.success("Organizador eliminado");
      setAEliminar(null);
      refrescar();
    },
    onError: (err) => {
      toast.error(err instanceof ApiError ? err.detail : "No se pudo eliminar el organizador");
    },
  });

  return (
    <div className="space-y-6 max-w-2xl">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ administración</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <Building2 className="w-7 h-7 text-violet" /> Organizadores
        </h1>
      </div>

      <div className="grid grid-cols-3 gap-3">
        <StatTile value={orgs?.length ?? "—"} label="Organizadores" color="violet" />
      </div>

      <HudPanel className="p-5">
        <HudEyebrow className="block mb-4">nuevo organizador</HudEyebrow>
        <form onSubmit={handleSubmit((d) => crear.mutate(d))} className="flex gap-3">
          <div className="flex-1 space-y-1.5">
            <Label className="eyebrow">Nombre</Label>
            <Input placeholder="Riot Games Latam" {...register("nombre")} />
            {errors.nombre && <p className="text-xs text-destructive">{errors.nombre.message}</p>}
          </div>
          <div className="pt-[22px]">
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
              Crear
            </Button>
          </div>
        </form>
        {serverError && (
          <div className="mt-3 rounded border border-destructive/40 bg-destructive/10 px-4 py-3">
            <p className="text-sm text-destructive">{serverError}</p>
          </div>
        )}
      </HudPanel>

      <HudPanel>
        <div className="px-4 py-3 border-b border-line">
          <HudEyebrow>todos los organizadores</HudEyebrow>
        </div>
        {isLoading ? (
          <div className="p-4 space-y-2">
            {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-12" />)}
          </div>
        ) : orgs?.length === 0 ? (
          <EmptyState title="Sin organizadores" description="Crea el primer organizador arriba." />
        ) : (
          <div className="divide-y divide-line">
            {orgs?.map((o) => (
              <div key={o.organizadorId} className="flex items-center justify-between px-4 py-3 gap-3">
                <p className="text-sm font-semibold text-foreground truncate">{o.nombre}</p>
                <div className="flex items-center gap-1 shrink-0">
                  <Button size="icon" variant="ghost" aria-label={`Editar ${o.nombre}`} onClick={() => setAEditar(o)}>
                    <Pencil className="w-4 h-4 text-violet" />
                  </Button>
                  <Button size="icon" variant="ghost" aria-label={`Eliminar ${o.nombre}`} onClick={() => setAEliminar(o)}>
                    <Trash2 className="w-4 h-4 text-destructive" />
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}
      </HudPanel>

      <EditarOrganizadorModal
        organizador={aEditar}
        onClose={() => setAEditar(null)}
        onGuardado={() => { setAEditar(null); refrescar(); }}
      />

      <Dialog open={!!aEliminar} onOpenChange={(o) => !o && setAEliminar(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <ShieldAlert className="w-5 h-5 text-destructive" /> Eliminar organizador
            </DialogTitle>
            <DialogDescription>
              Vas a eliminar <span className="font-semibold text-foreground">{aEliminar?.nombre}</span>.
              No se puede eliminar si tiene torneos asociados.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setAEliminar(null)}>Cancelar</Button>
            <Button
              variant="destructive"
              disabled={eliminar.isPending}
              onClick={() => aEliminar && eliminar.mutate(aEliminar.organizadorId)}
            >
              {eliminar.isPending && <Loader2 className="w-4 h-4 animate-spin" />}
              Eliminar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function EditarOrganizadorModal({
  organizador, onClose, onGuardado,
}: { organizador: OrganizadorResponse | null; onClose: () => void; onGuardado: () => void }) {
  const [serverError, setServerError] = useState<string | null>(null);
  const { register, handleSubmit, reset, formState: { errors } } = useForm<Form>({
    resolver: zodResolver(schema),
    values: organizador ? { nombre: organizador.nombre } : undefined,
  });

  const editar = useMutation({
    mutationFn: (d: Form) => editarOrganizador(organizador!.organizadorId, { nombre: d.nombre }),
    onSuccess: () => {
      toast.success("Organizador actualizado");
      setServerError(null);
      onGuardado();
    },
    onError: (err) => {
      setServerError(err instanceof ApiError ? err.detail : "No se pudo actualizar el organizador");
    },
  });

  const handleClose = () => { reset(); setServerError(null); onClose(); };

  return (
    <Dialog open={!!organizador} onOpenChange={(o) => !o && handleClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Editar organizador</DialogTitle>
          <DialogDescription>No se puede renombrar si ya tiene torneos asociados.</DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit((d) => editar.mutate(d))} className="space-y-4">
          <div className="space-y-1.5">
            <Label className="eyebrow">Nombre</Label>
            <Input {...register("nombre")} />
            {errors.nombre && <p className="text-xs text-destructive">{errors.nombre.message}</p>}
          </div>
          {serverError && (
            <div className="rounded border border-destructive/40 bg-destructive/10 px-4 py-3">
              <p className="text-sm text-destructive">{serverError}</p>
            </div>
          )}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose}>Cancelar</Button>
            <Button type="submit" disabled={editar.isPending}>
              {editar.isPending && <Loader2 className="w-4 h-4 animate-spin" />}
              Guardar
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
