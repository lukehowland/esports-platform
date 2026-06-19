"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Trophy, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth/context";
import { isOrganizador, isAdmin } from "@/lib/auth/types";
import { RequireRole } from "@/lib/auth/require-role";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { getOrganizadores, getVideojuegosPorGenero, crearTorneo } from "@/lib/api/torneos";
import { ApiError } from "@/lib/api/fetcher";

const GENEROS = ["FPS", "MOBA", "BATTLE_ROYALE", "RTS", "FIGHTING", "SPORTS", "RPG"];

const schema = z.object({
  nombre:        z.string().min(1, "Requerido").max(120),
  codigo:        z.string().min(1, "Requerido").max(20),
  genero:        z.string().min(1, "Requerido"),
  videojuegoId:  z.string().min(1, "Seleccioná un videojuego"),
  organizadorId: z.string().min(1, "Requerido"),
  fechaInicio:   z.string().min(1, "Requerido"),
  fechaFin:      z.string().min(1, "Requerido"),
});
type Form = z.infer<typeof schema>;

export default function CrearTorneoPage() {
  return (
    <RequireRole roles={["admin", "organizador"]}>
      <CrearTorneoContent />
    </RequireRole>
  );
}

function CrearTorneoContent() {
  const { identidad } = useAuth();
  const qc = useQueryClient();
  const router = useRouter();
  const [serverError, setServerError] = useState<string | null>(null);

  const orgId = isOrganizador(identidad) ? identidad.organizadorId : "";
  const esAdmin = isAdmin(identidad);

  const { register, handleSubmit, control, watch, setValue, formState: { errors, isSubmitting } } = useForm<Form>({
    resolver: zodResolver(schema),
    defaultValues: { genero: "FPS", organizadorId: orgId },
  });

  const generoActual = watch("genero");

  const { data: organizadores } = useQuery({
    queryKey: ["organizadores"],
    queryFn: getOrganizadores,
    enabled: esAdmin,
  });

  const { data: videojuegos } = useQuery({
    queryKey: ["videojuegos", generoActual],
    queryFn: () => getVideojuegosPorGenero(generoActual),
    enabled: !!generoActual,
  });

  const mutation = useMutation({
    mutationFn: ({ genero: _, ...data }: Form) =>
      crearTorneo({
        ...data,
        fechaInicio: new Date(data.fechaInicio).toISOString(),
        fechaFin: new Date(data.fechaFin).toISOString(),
      }),
    onSuccess: (torneo) => {
      toast.success("Torneo creado");
      qc.invalidateQueries({ queryKey: ["torneos"] });
      router.push(`/torneos/${torneo.torneoId}`);
    },
    onError: (err) => {
      setServerError(err instanceof ApiError ? err.detail : "Error al crear torneo");
    },
  });

  return (
    <div className="space-y-6 max-w-lg">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ gestión</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <Trophy className="w-7 h-7 text-violet" /> Nuevo Torneo
        </h1>
      </div>

      <HudPanel className="p-5">
        <HudEyebrow className="block mb-4">datos del torneo</HudEyebrow>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4">

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="space-y-1.5 sm:col-span-2">
              <Label className="eyebrow">Nombre del torneo</Label>
              <Input placeholder="Copa UNIVALLE 2026" {...register("nombre")} />
              {errors.nombre && <p className="text-xs text-destructive">{errors.nombre.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label className="eyebrow">Código único</Label>
              <Input placeholder="CU2026" {...register("codigo")} />
              {errors.codigo && <p className="text-xs text-destructive">{errors.codigo.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label className="eyebrow">Fecha de inicio</Label>
              <Input type="datetime-local" {...register("fechaInicio")} />
              {errors.fechaInicio && <p className="text-xs text-destructive">{errors.fechaInicio.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label className="eyebrow">Fecha de fin</Label>
              <Input type="datetime-local" {...register("fechaFin")} />
              {errors.fechaFin && <p className="text-xs text-destructive">{errors.fechaFin.message}</p>}
            </div>
          </div>

          <div className="space-y-1.5">
            <Label className="eyebrow">Género (filtro videojuego)</Label>
            <Controller control={control} name="genero" render={({ field }) => (
              <Select value={field.value} onValueChange={(v) => { field.onChange(v); setValue("videojuegoId", ""); }}>
                <SelectTrigger><SelectValue placeholder="Seleccioná un género…" /></SelectTrigger>
                <SelectContent>
                  {GENEROS.map((g) => <SelectItem key={g} value={g}>{g}</SelectItem>)}
                </SelectContent>
              </Select>
            )} />
          </div>

          <div className="space-y-1.5">
            <Label className="eyebrow">Videojuego</Label>
            <Controller control={control} name="videojuegoId" render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger><SelectValue placeholder="Seleccioná un videojuego…" /></SelectTrigger>
                <SelectContent>
                  {(videojuegos ?? []).map((v) => (
                    <SelectItem key={v.videojuegoId} value={v.videojuegoId}>{v.nombre}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )} />
            {errors.videojuegoId && <p className="text-xs text-destructive">{errors.videojuegoId.message}</p>}
          </div>

          {esAdmin && (
            <div className="space-y-1.5">
              <Label className="eyebrow">Organizador</Label>
              <Controller control={control} name="organizadorId" render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger><SelectValue placeholder="Seleccioná un organizador…" /></SelectTrigger>
                  <SelectContent>
                    {(organizadores ?? []).map((o) => (
                      <SelectItem key={o.organizadorId} value={o.organizadorId}>{o.nombre}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )} />
              {errors.organizadorId && <p className="text-xs text-destructive">{errors.organizadorId.message}</p>}
            </div>
          )}

          {serverError && (
            <div className="rounded border border-destructive/40 bg-destructive/10 px-4 py-3">
              <p className="text-sm text-destructive">{serverError}</p>
            </div>
          )}

          <Button type="submit" disabled={isSubmitting} className="w-full">
            {isSubmitting && <Loader2 className="w-4 h-4 animate-spin" />}
            <Trophy className="w-4 h-4" /> Crear torneo
          </Button>
        </form>
      </HudPanel>
    </div>
  );
}
