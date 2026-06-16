"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Gamepad2, Plus, ChevronDown, ChevronRight } from "lucide-react";
import { toast } from "sonner";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
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
        <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
          <Gamepad2 className="h-6 w-6 text-primary" /> Videojuegos
        </h1>
        {esOrganizador && (
          <CrearVideojuegoDialog onSuccess={() => qc.invalidateQueries({ queryKey: ["videojuegos", generoSeleccionado] })} />
        )}
      </div>

      {/* Selector de género */}
      <div className="flex flex-wrap gap-2">
        {GENEROS.map((g) => (
          <button
            key={g}
            onClick={() => setGeneroSeleccionado(g)}
            className={`rounded-full px-3 py-1 text-xs font-medium transition-colors border ${
              generoSeleccionado === g
                ? "bg-primary text-primary-foreground border-primary"
                : "border-border text-muted-foreground hover:text-foreground hover:border-foreground"
            }`}
          >
            {g}
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-32" />)}
        </div>
      ) : error ? <ErrorState error={error} onRetry={refetch} /> :
      data?.length === 0 ? (
        <EmptyState title={`Sin videojuegos en ${generoSeleccionado}`} description="No hay videojuegos registrados en este género." />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {data?.map((vg) => (
            <Card key={vg.videojuegoId}>
              <CardHeader className="pb-2">
                <div className="flex items-center gap-2">
                  <Gamepad2 className="h-4 w-4 text-primary" />
                  <CardTitle className="text-sm">{vg.nombre}</CardTitle>
                </div>
                <Badge variant="secondary" className="w-fit text-xs">{generoSeleccionado}</Badge>
              </CardHeader>
              <CardContent>
                <TorneosPorVideojuego videojuegoId={vg.videojuegoId} nombre={vg.nombre} />
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
