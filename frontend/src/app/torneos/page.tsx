"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Trophy, Plus, Search } from "lucide-react";
import { toast } from "sonner";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { getTorneosPorFecha, getTorneoPorCodigo, crearTorneo, getOrganizadores, getVideojuegosPorGenero } from "@/lib/api/torneos";
import { useAuth } from "@/lib/auth/context";
import { isOrganizador } from "@/lib/auth/types";
import { formatDate } from "@/lib/utils";
import type { ApiError } from "@/lib/api/fetcher";
import { ApiError as AE } from "@/lib/api/fetcher";

const GENEROS = ["MOBA", "FPS", "BATTLE_ROYALE", "RTS", "FIGHTING", "SPORTS", "RPG"];

const torneoSchema = z.object({
  nombre: z.string().min(1, "Requerido"),
  codigo: z.string().min(1, "Requerido"),
  videojuegoId: z.string().min(1, "Requerido"),
  organizadorId: z.string().min(1, "Requerido"),
  fechaInicio: z.string().min(1, "Requerido"),
  genero: z.string().min(1, "Requerido"),
});
type TorneoForm = z.infer<typeof torneoSchema>;

function CrearTorneoDialog({ organizadorIdPorDefecto }: { organizadorIdPorDefecto?: string }) {
  const [open, setOpen] = useState(false);
  const [generoLocal, setGeneroLocal] = useState("MOBA");
  const qc = useQueryClient();

  const { register, handleSubmit, control, setValue, watch, formState: { errors }, reset } = useForm<TorneoForm>({
    resolver: zodResolver(torneoSchema),
    defaultValues: { organizadorId: organizadorIdPorDefecto ?? "", genero: "MOBA" },
  });

  const { data: organizadores } = useQuery({ queryKey: ["organizadores"], queryFn: getOrganizadores });
  const { data: videojuegos } = useQuery({
    queryKey: ["videojuegos", generoLocal],
    queryFn: () => getVideojuegosPorGenero(generoLocal),
    enabled: !!generoLocal,
  });

  const mutation = useMutation({
    mutationFn: ({ genero: _, ...data }: TorneoForm) =>
      crearTorneo({ ...data, fechaInicio: new Date(data.fechaInicio).toISOString() }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["torneos"] });
      toast.success("Torneo creado");
      setOpen(false);
      reset();
    },
    onError: (e: ApiError) => toast.error(e.detail),
  });

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm"><Plus className="h-4 w-4 mr-1" />Crear torneo</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader><DialogTitle>Nuevo torneo</DialogTitle></DialogHeader>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4 mt-2">
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2 space-y-1">
              <Label>Nombre del torneo</Label>
              <Input {...register("nombre")} placeholder="Copa UNIVALLE 2026" />
              {errors.nombre && <p className="text-xs text-destructive">{errors.nombre.message}</p>}
            </div>
            <div className="space-y-1">
              <Label>Código único</Label>
              <Input {...register("codigo")} placeholder="CU2026" />
              {errors.codigo && <p className="text-xs text-destructive">{errors.codigo.message}</p>}
            </div>
            <div className="space-y-1">
              <Label>Fecha de inicio</Label>
              <Input {...register("fechaInicio")} type="datetime-local" />
              {errors.fechaInicio && <p className="text-xs text-destructive">{errors.fechaInicio.message}</p>}
            </div>
          </div>

          <div className="space-y-1">
            <Label>Organizador</Label>
            <Controller control={control} name="organizadorId" render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger><SelectValue placeholder="Seleccionar organizador…" /></SelectTrigger>
                <SelectContent>
                  {organizadores?.map((o) => <SelectItem key={o.organizadorId} value={o.organizadorId}>{o.nombre}</SelectItem>)}
                </SelectContent>
              </Select>
            )} />
            {errors.organizadorId && <p className="text-xs text-destructive">{errors.organizadorId.message}</p>}
          </div>

          <div className="space-y-1">
            <Label>Género del videojuego</Label>
            <Select value={generoLocal} onValueChange={(v) => { setGeneroLocal(v); setValue("genero", v); setValue("videojuegoId", ""); }}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>{GENEROS.map((g) => <SelectItem key={g} value={g}>{g}</SelectItem>)}</SelectContent>
            </Select>
          </div>

          <div className="space-y-1">
            <Label>Videojuego</Label>
            <Controller control={control} name="videojuegoId" render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange} disabled={!videojuegos?.length}>
                <SelectTrigger><SelectValue placeholder="Seleccionar videojuego…" /></SelectTrigger>
                <SelectContent>
                  {videojuegos?.map((vg) => <SelectItem key={vg.videojuegoId} value={vg.videojuegoId}>{vg.nombre}</SelectItem>)}
                </SelectContent>
              </Select>
            )} />
            {errors.videojuegoId && <p className="text-xs text-destructive">{errors.videojuegoId.message}</p>}
          </div>

          <Button type="submit" className="w-full" disabled={mutation.isPending}>
            {mutation.isPending ? "Creando…" : "Crear torneo"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
}

export default function TorneosPage() {
  const [codigo, setCodigo] = useState("");
  const [codigoBuscado, setCodigoBuscado] = useState("");
  const { identidad } = useAuth();
  const esOrg = isOrganizador(identidad);

  const { data: torneos, isLoading, error, refetch } = useQuery({
    queryKey: ["torneos", "por-fecha"],
    queryFn: getTorneosPorFecha,
  });

  const { data: torneoPorCodigo, isLoading: buscandoCodigo, error: errorCodigo } = useQuery({
    queryKey: ["torneo", "codigo", codigoBuscado],
    queryFn: () => getTorneoPorCodigo(codigoBuscado),
    enabled: !!codigoBuscado,
    retry: false,
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <div>
          <p className="eyebrow text-lime mb-1">▰▰ competencia</p>
          <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
            <Trophy className="w-7 h-7 text-lime" /> Torneos
          </h1>
        </div>
        {esOrg && <CrearTorneoDialog organizadorIdPorDefecto={isOrganizador(identidad) ? identidad.organizadorId : undefined} />}
      </div>

      {/* Buscar por código (Q15) */}
      <div className="space-y-2">
        <p className="text-xs text-muted-foreground font-medium">Buscar por código único</p>
        <div className="flex gap-2 max-w-sm">
          <Input
            placeholder="ej: WORLDS25"
            value={codigo}
            onChange={(e) => setCodigo(e.target.value.toUpperCase())}
            onKeyDown={(e) => e.key === "Enter" && setCodigoBuscado(codigo.trim())}
            className="font-mono"
          />
          <Button variant="outline" size="icon" onClick={() => setCodigoBuscado(codigo.trim())}>
            <Search className="h-4 w-4" />
          </Button>
        </div>
        {codigoBuscado && (
          buscandoCodigo ? <Skeleton className="h-16 w-full max-w-sm" /> :
          errorCodigo instanceof AE && errorCodigo.status === 404 ? (
            <p className="text-sm text-muted-foreground">No se encontró el torneo con código <span className="font-mono text-lime">{codigoBuscado}</span>.</p>
          ) : torneoPorCodigo ? (
            <Link href={`/torneos/${torneoPorCodigo.torneoId}`}>
              <HudPanel className="max-w-sm p-4 hover:border-lime/50 transition-colors cursor-pointer">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="font-semibold text-foreground">{torneoPorCodigo.nombre}</p>
                    <p className="eyebrow mt-0.5">{formatDate(torneoPorCodigo.fechaInicio)}</p>
                  </div>
                  <span className="hud-clip-sm border border-lime/30 bg-lime/10 text-lime font-mono text-xs px-2 py-0.5">{codigoBuscado}</span>
                </div>
              </HudPanel>
            </Link>
          ) : null
        )}
      </div>

      {/* Lista por fecha (Q12) */}
      <HudPanel>
        <div className="px-4 py-3 border-b border-line">
          <HudEyebrow>{torneos?.length ?? "…"} torneos por fecha</HudEyebrow>
        </div>
        {isLoading ? (
          <div className="p-4 space-y-2">{[...Array(5)].map((_, i) => <Skeleton key={i} className="h-12" />)}</div>
        ) : error ? <ErrorState error={error} onRetry={refetch} /> :
        torneos?.length === 0 ? <EmptyState title="Sin torneos" description="No hay torneos registrados." /> : (
          <div className="divide-y divide-line">
            {torneos?.map((t) => (
              <Link key={t.torneoId} href={`/torneos/${t.torneoId}`}
                className="flex items-center justify-between px-4 py-3 hover:bg-secondary/40 transition-colors"
              >
                <div>
                  <p className="text-sm font-semibold text-foreground">{t.nombreTorneo}</p>
                  <p className="eyebrow mt-0.5">{t.nombreVideojuego}</p>
                </div>
                <span className="eyebrow shrink-0">{formatDate(t.fechaInicio)}</span>
              </Link>
            ))}
          </div>
        )}
      </HudPanel>
    </div>
  );
}
