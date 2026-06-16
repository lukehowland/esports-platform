"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Plus, Search, Users } from "lucide-react";
import Link from "next/link";
import { toast } from "sonner";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { getEquiposPorFecha, getEquipoPorTag, crearEquipo } from "@/lib/api/equipos";
import { useAuth } from "@/lib/auth/context";
import { isCapitan } from "@/lib/auth/types";
import { formatDate } from "@/lib/utils";
import type { ApiError } from "@/lib/api/fetcher";

const equipoSchema = z.object({
  nombre: z.string().min(1, "Requerido").max(100),
  tag: z.string().min(1, "Requerido").max(10, "Máx 10 caracteres"),
  pais: z.string().min(1, "Requerido"),
});
type EquipoForm = z.infer<typeof equipoSchema>;

function CrearEquipoDialog() {
  const [open, setOpen] = useState(false);
  const qc = useQueryClient();
  const { register, handleSubmit, formState: { errors }, reset } = useForm<EquipoForm>({
    resolver: zodResolver(equipoSchema),
  });

  const mutation = useMutation({
    mutationFn: crearEquipo,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["equipos"] });
      toast.success("Equipo creado");
      setOpen(false);
      reset();
    },
    onError: (e: ApiError) => toast.error(e.detail),
  });

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm"><Plus className="h-4 w-4 mr-1" />Crear equipo</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader><DialogTitle>Nuevo equipo</DialogTitle></DialogHeader>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4 mt-2">
          <div className="space-y-1">
            <Label>Nombre del equipo</Label>
            <Input {...register("nombre")} placeholder="Tigres eSports" />
            {errors.nombre && <p className="text-xs text-destructive">{errors.nombre.message}</p>}
          </div>
          <div className="space-y-1">
            <Label>Tag (identificador corto)</Label>
            <Input {...register("tag")} placeholder="TIG" />
            {errors.tag && <p className="text-xs text-destructive">{errors.tag.message}</p>}
          </div>
          <div className="space-y-1">
            <Label>País</Label>
            <Input {...register("pais")} placeholder="Bolivia" />
            {errors.pais && <p className="text-xs text-destructive">{errors.pais.message}</p>}
          </div>
          <Button type="submit" className="w-full" disabled={mutation.isPending}>
            {mutation.isPending ? "Creando…" : "Crear equipo"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
}

export default function EquiposPage() {
  const [busquedaTag, setBusquedaTag] = useState("");
  const [tagBuscado, setTagBuscado] = useState("");
  const { identidad } = useAuth();
  const esCapitan = isCapitan(identidad);

  const { data: equipos, isLoading, error, refetch } = useQuery({
    queryKey: ["equipos", "por-fecha"],
    queryFn: getEquiposPorFecha,
  });

  const { data: equipoPorTag, isLoading: buscandoTag } = useQuery({
    queryKey: ["equipos", "por-tag", tagBuscado],
    queryFn: () => getEquipoPorTag(tagBuscado),
    enabled: !!tagBuscado,
    retry: false,
  });

  const handleBuscar = () => setTagBuscado(busquedaTag.trim());

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {[...Array(6)].map((_, i) => <Skeleton key={i} className="h-32" />)}
        </div>
      </div>
    );
  }

  if (error) return <ErrorState error={error} onRetry={refetch} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
          <Users className="h-6 w-6 text-primary" /> Equipos
        </h1>
        {esCapitan && <CrearEquipoDialog />}
      </div>

      {/* Buscar por tag (Q5) */}
      <div className="flex gap-2 max-w-sm">
        <Input
          placeholder="Buscar por tag… (ej: T1)"
          value={busquedaTag}
          onChange={(e) => setBusquedaTag(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && handleBuscar()}
        />
        <Button variant="outline" size="icon" onClick={handleBuscar}>
          <Search className="h-4 w-4" />
        </Button>
      </div>

      {/* Resultado de búsqueda por tag */}
      {tagBuscado && (
        <div>
          <p className="text-sm text-muted-foreground mb-2">
            Resultado para tag <span className="text-primary font-mono">{tagBuscado}</span>:
          </p>
          {buscandoTag ? (
            <Skeleton className="h-20 w-full max-w-sm" />
          ) : equipoPorTag ? (
            <Link href={`/equipos/${equipoPorTag.equipoId}`}>
              <Card className="max-w-sm hover:border-primary/40 transition-colors cursor-pointer">
                <CardHeader className="py-3">
                  <div className="flex items-center gap-2">
                    <Badge variant="secondary" className="font-mono">{equipoPorTag.tag}</Badge>
                    <CardTitle className="text-sm">{equipoPorTag.nombre}</CardTitle>
                  </div>
                  <p className="text-xs text-muted-foreground">{equipoPorTag.pais}</p>
                </CardHeader>
              </Card>
            </Link>
          ) : (
            <p className="text-sm text-muted-foreground">No encontrado.</p>
          )}
        </div>
      )}

      {/* Lista por fecha (Q4) */}
      <div>
        <p className="text-xs text-muted-foreground mb-3">
          {equipos?.length ?? 0} equipos registrados, ordenados por fecha de creación
        </p>
        {equipos?.length === 0 ? (
          <EmptyState title="Sin equipos" description="No hay equipos registrados. Sé el primero en crear uno." />
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {equipos?.map((equipo) => (
              <Link key={equipo.equipoId} href={`/equipos/${equipo.equipoId}`}>
                <Card className="h-full hover:border-primary/40 transition-colors cursor-pointer group">
                  <CardHeader className="pb-2">
                    <div className="flex items-center gap-2">
                      <Badge variant="secondary" className="font-mono text-primary shrink-0">{equipo.tag}</Badge>
                      <CardTitle className="text-sm group-hover:text-primary transition-colors truncate">{equipo.nombre}</CardTitle>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <p className="text-xs text-muted-foreground">{equipo.pais}</p>
                    <p className="text-xs text-muted-foreground mt-1">{formatDate(equipo.fechaCreacion)}</p>
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
