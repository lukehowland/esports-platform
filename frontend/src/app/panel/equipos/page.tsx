"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import Link from "next/link";
import { Users, Flag, ChevronRight, Plus, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { RequireRole } from "@/lib/auth/require-role";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { StatTile } from "@/components/stat-tile";
import { EmptyState } from "@/components/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getEquiposPorFecha, crearEquipo } from "@/lib/api/equipos";
import { ApiError } from "@/lib/api/fetcher";
import { formatDate } from "@/lib/utils";

const schema = z.object({
  nombre: z.string().min(1, "Requerido").max(120),
  tag: z.string().min(1, "Requerido").max(16),
  pais: z.string().min(1, "Requerido").max(32),
});
type Form = z.infer<typeof schema>;

export default function PanelEquiposPage() {
  return (
    <RequireRole roles={["admin"]}>
      <EquiposContent />
    </RequireRole>
  );
}

function EquiposContent() {
  const qc = useQueryClient();
  const { data: equipos, isLoading } = useQuery({
    queryKey: ["equipos", "por-fecha"],
    queryFn: getEquiposPorFecha,
  });

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<Form>({
    resolver: zodResolver(schema),
  });

  const crear = useMutation({
    mutationFn: (d: Form) => crearEquipo(d),
    onSuccess: () => {
      toast.success("Equipo creado");
      reset();
      qc.invalidateQueries({ queryKey: ["equipos"] });
    },
    onError: (e) => toast.error(e instanceof ApiError ? e.detail : "No se pudo crear el equipo"),
  });

  return (
    <div className="space-y-6 max-w-2xl">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ administración</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <Users className="w-7 h-7 text-violet" /> Equipos
        </h1>
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        <StatTile value={equipos?.length ?? "—"} label="Total equipos" color="violet" />
      </div>

      <HudPanel className="p-5">
        <HudEyebrow className="block mb-4">nuevo equipo</HudEyebrow>
        <form onSubmit={handleSubmit((d) => crear.mutate(d))} className="space-y-3">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div className="space-y-1.5">
              <Label className="eyebrow">Nombre</Label>
              <Input placeholder="Cloud9" {...register("nombre")} />
              {errors.nombre && <p className="text-xs text-destructive">{errors.nombre.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label className="eyebrow">Tag</Label>
              <Input placeholder="C9" {...register("tag")} />
              {errors.tag && <p className="text-xs text-destructive">{errors.tag.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label className="eyebrow">País (ISO-2)</Label>
              <Input placeholder="US" {...register("pais")} />
              {errors.pais && <p className="text-xs text-destructive">{errors.pais.message}</p>}
            </div>
          </div>
          <Button type="submit" disabled={isSubmitting || crear.isPending}>
            {crear.isPending ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
            Crear equipo
          </Button>
        </form>
      </HudPanel>

      <HudPanel>
        <div className="px-4 py-3 border-b border-line">
          <HudEyebrow>todos los equipos</HudEyebrow>
        </div>
        {isLoading ? (
          <div className="p-4 space-y-2">
            {[...Array(5)].map((_, i) => <Skeleton key={i} className="h-12" />)}
          </div>
        ) : equipos?.length === 0 ? (
          <EmptyState title="Sin equipos" description="No hay equipos registrados todavía." />
        ) : (
          <div className="divide-y divide-line">
            {equipos?.map((e) => (
              <Link
                key={e.equipoId}
                href={`/panel/equipos/${e.equipoId}`}
                className="flex items-center justify-between px-4 py-3 hover:bg-secondary/40 transition-colors"
              >
                <div className="flex items-center gap-3">
                  <span className="hud-clip-sm border border-violet/30 bg-violet/10 text-violet text-xs font-mono px-2 py-0.5">
                    {e.tag}
                  </span>
                  <div>
                    <p className="text-sm font-semibold text-foreground">{e.nombre}</p>
                    <p className="eyebrow mt-0.5 flex items-center gap-1">
                      <Flag className="w-3 h-3" /> {e.pais}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <span className="eyebrow">{formatDate(e.fechaCreacion)}</span>
                  <ChevronRight className="w-4 h-4 text-muted-foreground" />
                </div>
              </Link>
            ))}
          </div>
        )}
      </HudPanel>
    </div>
  );
}
