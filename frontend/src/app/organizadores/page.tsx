"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Building2, Plus, ChevronDown, ChevronRight } from "lucide-react";
import { toast } from "sonner";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { getOrganizadores, getTorneosPorOrganizador, crearOrganizador } from "@/lib/api/torneos";
import { useAuth } from "@/lib/auth/context";
import { isOrganizador } from "@/lib/auth/types";
import { formatDate } from "@/lib/utils";
import type { ApiError } from "@/lib/api/fetcher";

const orgSchema = z.object({ nombre: z.string().min(1, "Requerido") });
type OrgForm = z.infer<typeof orgSchema>;

function CrearOrganizadorDialog({ onSuccess }: { onSuccess: () => void }) {
  const [open, setOpen] = useState(false);
  const { register, handleSubmit, formState: { errors }, reset } = useForm<OrgForm>({
    resolver: zodResolver(orgSchema),
  });
  const mutation = useMutation({
    mutationFn: crearOrganizador,
    onSuccess: () => { toast.success("Organizador creado"); setOpen(false); reset(); onSuccess(); },
    onError: (e: ApiError) => toast.error(e.detail),
  });
  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm"><Plus className="h-4 w-4 mr-1" />Crear organizador</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader><DialogTitle>Nuevo organizador</DialogTitle></DialogHeader>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4 mt-2">
          <div className="space-y-1">
            <Label>Nombre del organizador</Label>
            <Input {...register("nombre")} placeholder="ESL Gaming" />
            {errors.nombre && <p className="text-xs text-destructive">{errors.nombre.message}</p>}
          </div>
          <Button type="submit" className="w-full" disabled={mutation.isPending}>
            {mutation.isPending ? "Creando…" : "Crear organizador"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function TorneosOrganizador({ organizadorId, nombre }: { organizadorId: string; nombre: string }) {
  const [exp, setExp] = useState(false);
  const { data, isLoading } = useQuery({
    queryKey: ["torneos", "por-org", organizadorId],
    queryFn: () => getTorneosPorOrganizador(organizadorId),
    enabled: exp,
  });
  return (
    <div>
      <button onClick={() => setExp(!exp)} className="flex items-center gap-1.5 text-xs text-primary hover:underline mt-2">
        {exp ? <ChevronDown className="h-3 w-3" /> : <ChevronRight className="h-3 w-3" />}
        Ver torneos de {nombre}
      </button>
      {exp && (
        <div className="mt-2 pl-2 border-l border-border space-y-1">
          {isLoading ? <Skeleton className="h-12" /> :
            data?.length === 0 ? <p className="text-xs text-muted-foreground">Sin torneos.</p> :
            data?.map((t) => (
              <div key={t.torneoId} className="flex items-center justify-between py-1">
                <span className="text-xs text-foreground">{t.nombreTorneo}</span>
                <div className="flex items-center gap-2">
                  <span className="text-xs text-muted-foreground">{t.nombreVideojuego}</span>
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

export default function OrganizadoresPage() {
  const { identidad } = useAuth();
  const esOrg = isOrganizador(identidad);
  const qc = useQueryClient();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["organizadores"],
    queryFn: getOrganizadores,
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
          <Building2 className="h-6 w-6 text-primary" /> Organizadores
        </h1>
        {esOrg && <CrearOrganizadorDialog onSuccess={() => qc.invalidateQueries({ queryKey: ["organizadores"] })} />}
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {[...Array(3)].map((_, i) => <Skeleton key={i} className="h-28" />)}
        </div>
      ) : error ? <ErrorState error={error} onRetry={refetch} /> :
      data?.length === 0 ? <EmptyState title="Sin organizadores" description="No hay organizadores registrados." /> : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {data?.map((org) => (
            <Card key={org.organizadorId}>
              <CardHeader className="pb-2">
                <div className="flex items-center gap-2">
                  <Building2 className="h-4 w-4 text-primary" />
                  <CardTitle className="text-sm">{org.nombre}</CardTitle>
                </div>
              </CardHeader>
              <CardContent>
                <TorneosOrganizador organizadorId={org.organizadorId} nombre={org.nombre} />
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
