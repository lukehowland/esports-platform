"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Gamepad2, Loader2, Plus } from "lucide-react";
import { toast } from "sonner";
import { RequireRole } from "@/lib/auth/require-role";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { getVideojuegosPorGenero, crearVideojuego } from "@/lib/api/torneos";
import { ApiError } from "@/lib/api/fetcher";

const GENEROS = ["FPS", "MOBA", "BATTLE_ROYALE", "RTS", "FIGHTING", "SPORTS", "RPG"];

const schema = z.object({
  nombre: z.string().min(1, "Requerido").max(120),
  genero: z.string().min(1, "Requerido"),
});
type Form = z.infer<typeof schema>;

export default function PanelVideojuegosPage() {
  return (
    <RequireRole roles={["admin", "organizador"]}>
      <VideojuegosContent />
    </RequireRole>
  );
}

function VideojuegosContent() {
  const qc = useQueryClient();
  const [genero, setGenero] = useState("FPS");
  const [serverError, setServerError] = useState<string | null>(null);

  const { data: videojuegos, isLoading } = useQuery({
    queryKey: ["videojuegos", genero],
    queryFn: () => getVideojuegosPorGenero(genero),
  });

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<Form>({
    resolver: zodResolver(schema),
    defaultValues: { genero: "FPS" },
  });

  const mutation = useMutation({
    mutationFn: (d: Form) => crearVideojuego({ nombre: d.nombre, genero: d.genero }),
    onSuccess: () => {
      toast.success("Videojuego creado");
      reset({ genero: "FPS" });
      setServerError(null);
      qc.invalidateQueries({ queryKey: ["videojuegos"] });
    },
    onError: (err) => {
      setServerError(err instanceof ApiError ? err.detail : "Error al crear videojuego");
    },
  });

  return (
    <div className="space-y-6 max-w-2xl">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ catálogo</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <Gamepad2 className="w-7 h-7 text-violet" /> Videojuegos
        </h1>
      </div>

      <HudPanel className="p-5">
        <HudEyebrow className="block mb-4">nuevo videojuego</HudEyebrow>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label className="eyebrow">Nombre</Label>
              <Input placeholder="Counter-Strike 2" {...register("nombre")} />
              {errors.nombre && <p className="text-xs text-destructive">{errors.nombre.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label className="eyebrow">Género</Label>
              <Input placeholder="FPS" {...register("genero")} />
              {errors.genero && <p className="text-xs text-destructive">{errors.genero.message}</p>}
            </div>
          </div>
          {serverError && (
            <div className="rounded border border-destructive/40 bg-destructive/10 px-4 py-3">
              <p className="text-sm text-destructive">{serverError}</p>
            </div>
          )}
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
            Crear videojuego
          </Button>
        </form>
      </HudPanel>

      <HudPanel>
        <div className="px-4 py-3 border-b border-line flex items-center justify-between">
          <HudEyebrow>buscar por género</HudEyebrow>
        </div>
        <div className="px-4 py-3 flex flex-wrap gap-1.5 border-b border-line">
          {GENEROS.map((g) => (
            <button
              key={g}
              onClick={() => setGenero(g)}
              className={`eyebrow px-2 py-0.5 rounded hud-clip-sm border text-xs transition-colors ${genero === g ? "border-violet/60 bg-violet/15 text-violet" : "border-line bg-elevated text-muted-foreground hover:text-foreground hover:border-violet/30"}`}
            >
              {g}
            </button>
          ))}
        </div>
        {isLoading ? (
          <div className="p-4 space-y-2">
            {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-10" />)}
          </div>
        ) : videojuegos?.length === 0 ? (
          <EmptyState title="Sin resultados" description={`No hay videojuegos de género ${genero}.`} />
        ) : (
          <div className="divide-y divide-line">
            {videojuegos?.map((v) => (
              <div key={v.videojuegoId} className="flex items-center justify-between px-4 py-3">
                <p className="text-sm font-semibold text-foreground">{v.nombre}</p>
                <span className="hud-clip-sm border border-lime/30 bg-lime/10 text-lime text-xs font-mono px-2 py-0.5">
                  {genero}
                </span>
              </div>
            ))}
          </div>
        )}
      </HudPanel>
    </div>
  );
}
