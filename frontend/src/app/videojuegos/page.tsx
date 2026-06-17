"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Gamepad2, Plus, ChevronDown, ChevronRight } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { getVideojuegosPorGenero, getTorneosPorVideojuego, crearVideojuego } from "@/lib/api/torneos";
import { useAuth } from "@/lib/auth/context";
import { isOrganizador } from "@/lib/auth/types";
import { formatDate } from "@/lib/utils";
import type { ApiError } from "@/lib/api/fetcher";

const GENEROS = ["MOBA", "FPS", "BATTLE_ROYALE", "RTS", "FIGHTING", "SPORTS", "RPG"];

const vgSchema = z.object({
  nombre: z.string().min(1, "Requerido"),
  genero: z.string().min(1, "Requerido"),
});
type VgForm = z.infer<typeof vgSchema>;

function CrearVideojuegoDialog({ onSuccess }: { onSuccess: () => void }) {
  const [open, setOpen] = useState(false);
  const { register, handleSubmit, setValue, formState: { errors }, reset } = useForm<VgForm>({
    resolver: zodResolver(vgSchema),
  });

  const mutation = useMutation({
    mutationFn: crearVideojuego,
    onSuccess: () => {
      toast.success("Videojuego creado");
      setOpen(false);
      reset();
      onSuccess();
    },
    onError: (e: ApiError) => toast.error(e.detail),
  });

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm"><Plus className="h-4 w-4 mr-1" />Crear videojuego</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader><DialogTitle>Nuevo videojuego</DialogTitle></DialogHeader>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4 mt-2">
          <div className="space-y-1">
            <Label>Nombre</Label>
            <Input {...register("nombre")} placeholder="League of Legends" />
            {errors.nombre && <p className="text-xs text-destructive">{errors.nombre.message}</p>}
          </div>
          <div className="space-y-1">
            <Label>Género</Label>
            <Select onValueChange={(v) => setValue("genero", v)}>
              <SelectTrigger><SelectValue placeholder="Seleccionar género…" /></SelectTrigger>
              <SelectContent>
                {GENEROS.map((g) => <SelectItem key={g} value={g}>{g}</SelectItem>)}
              </SelectContent>
            </Select>
            {errors.genero && <p className="text-xs text-destructive">{errors.genero.message}</p>}
          </div>
          <Button type="submit" className="w-full" disabled={mutation.isPending}>
            {mutation.isPending ? "Creando…" : "Crear videojuego"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
}

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
  const [generoSeleccionado, setGeneroSeleccionado] = useState("MOBA");
  const { identidad } = useAuth();
  const esOrganizador = isOrganizador(identidad);
  const qc = useQueryClient();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["videojuegos", generoSeleccionado],
    queryFn: () => getVideojuegosPorGenero(generoSeleccionado),
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <div>
          <p className="eyebrow text-violet mb-1">▰▰ catálogo</p>
          <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
            <Gamepad2 className="w-7 h-7 text-violet" /> Videojuegos
          </h1>
        </div>
        {esOrganizador && (
          <CrearVideojuegoDialog onSuccess={() => qc.invalidateQueries({ queryKey: ["videojuegos", generoSeleccionado] })} />
        )}
      </div>

      {/* Selector de género */}
      <div className="flex flex-wrap gap-1.5">
        {GENEROS.map((g) => (
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
          <HudEyebrow>{generoSeleccionado} — {data?.length ?? "…"} juegos</HudEyebrow>
        </div>
        {isLoading ? (
          <div className="p-4 space-y-2">{[...Array(4)].map((_, i) => <Skeleton key={i} className="h-16" />)}</div>
        ) : error ? <ErrorState error={error} onRetry={refetch} /> :
        data?.length === 0 ? (
          <EmptyState title={`Sin videojuegos en ${generoSeleccionado}`} description="No hay videojuegos registrados en este género." />
        ) : (
          <div className="divide-y divide-line">
            {data?.map((vg) => (
              <div key={vg.videojuegoId} className="px-4 py-3">
                <div className="flex items-center gap-2 mb-1">
                  <Gamepad2 className="h-4 w-4 text-violet" />
                  <p className="font-semibold text-foreground">{vg.nombre}</p>
                  <span className="hud-clip-sm border border-violet/30 bg-violet/10 text-violet font-mono text-xs px-2 py-0.5 ml-auto">
                    {generoSeleccionado}
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
