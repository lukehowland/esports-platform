"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Building2, Loader2, Plus } from "lucide-react";
import { toast } from "sonner";
import Link from "next/link";
import { RequireRole } from "@/lib/auth/require-role";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { StatTile } from "@/components/stat-tile";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { getOrganizadores, crearOrganizador } from "@/lib/api/torneos";
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

  const { data: orgs, isLoading } = useQuery({
    queryKey: ["organizadores"],
    queryFn: getOrganizadores,
  });

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<Form>({
    resolver: zodResolver(schema),
  });

  const mutation = useMutation({
    mutationFn: (d: Form) => crearOrganizador({ nombre: d.nombre }),
    onSuccess: () => {
      toast.success("Organizador creado");
      reset();
      setServerError(null);
      qc.invalidateQueries({ queryKey: ["organizadores"] });
    },
    onError: (err) => {
      setServerError(err instanceof ApiError ? err.detail : "Error al crear organizador");
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
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="flex gap-3">
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
              <Link
                key={o.organizadorId}
                href={`/organizadores/${o.organizadorId}`}
                className="flex items-center justify-between px-4 py-3 hover:bg-secondary/40 transition-colors"
              >
                <p className="text-sm font-semibold text-foreground">{o.nombre}</p>
                <span className="eyebrow text-violet">ver torneos →</span>
              </Link>
            ))}
          </div>
        )}
      </HudPanel>
    </div>
  );
}
