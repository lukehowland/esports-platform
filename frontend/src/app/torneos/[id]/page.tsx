"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Trophy, Plus, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { ResultadoBadge } from "@/components/resultado-badge";
import {
  getTorneoPorId, getEquiposPorTorneo, getPremiosPorTorneo,
  inscribirEquipo, asignarPremio, getOrganizadores
} from "@/lib/api/torneos";
import { getEquiposPorFecha } from "@/lib/api/equipos";
import { getPartidasPorTorneo, registrarPartida } from "@/lib/api/partidas";
import { useAuth } from "@/lib/auth/context";
import { isOrganizador, isCapitan } from "@/lib/auth/types";
import { formatDate, formatDateTime, formatCurrency } from "@/lib/utils";
import type { ApiError } from "@/lib/api/fetcher";

// Inscribir equipo
function InscribirEquipoDialog({ torneoId }: { torneoId: string }) {
  const [open, setOpen] = useState(false);
  const [equipoId, setEquipoId] = useState("");
  const qc = useQueryClient();
  const { identidad } = useAuth();
  const { data: equipos } = useQuery({ queryKey: ["equipos", "por-fecha"], queryFn: getEquiposPorFecha });

  const mutation = useMutation({
    mutationFn: () => inscribirEquipo(torneoId, equipoId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["torneo", torneoId, "equipos"] });
      toast.success("Equipo inscrito. Rankings se actualizarán en breve.");
      setOpen(false);
      setEquipoId("");
    },
    onError: (e: ApiError) => toast.error(e.detail),
  });

  const miEquipoId = isCapitan(identidad) ? identidad.equipoId : "";

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm" variant="outline"><Plus className="h-4 w-4 mr-1" />Inscribir equipo</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader><DialogTitle>Inscribir equipo en el torneo</DialogTitle></DialogHeader>
        <div className="space-y-4 mt-2">
          <div className="space-y-1">
            <Label>Equipo</Label>
            <Select value={equipoId} onValueChange={setEquipoId} defaultValue={miEquipoId}>
              <SelectTrigger><SelectValue placeholder="Seleccionar equipo…" /></SelectTrigger>
              <SelectContent>
                {equipos?.map((e) => (
                  <SelectItem key={e.equipoId} value={e.equipoId}>[{e.tag}] {e.nombre}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <Button className="w-full" onClick={() => mutation.mutate()} disabled={!equipoId || mutation.isPending}>
            {mutation.isPending ? "Inscribiendo…" : "Inscribir"}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

// Asignar premio
const premioSchema = z.object({
  monto: z.coerce.number().positive("Debe ser positivo"),
  tipo: z.string().min(1, "Requerido"),
  equipoId: z.string().optional(),
});
type PremioForm = z.infer<typeof premioSchema>;

function AsignarPremioDialog({ torneoId }: { torneoId: string }) {
  const [open, setOpen] = useState(false);
  const qc = useQueryClient();
  const { data: equiposTorneo } = useQuery({
    queryKey: ["torneo", torneoId, "equipos"],
    queryFn: () => getEquiposPorTorneo(torneoId),
  });
  const { register, handleSubmit, control, formState: { errors }, reset } = useForm<PremioForm>({
    resolver: zodResolver(premioSchema),
  });
  const mutation = useMutation({
    mutationFn: (data: PremioForm) => asignarPremio(torneoId, { ...data, equipoId: data.equipoId || undefined }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["torneo", torneoId, "premios"] });
      toast.success("Premio asignado");
      setOpen(false);
      reset();
    },
    onError: (e: ApiError) => toast.error(e.detail),
  });

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm" variant="outline"><Plus className="h-4 w-4 mr-1" />Asignar premio</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader><DialogTitle>Asignar premio al torneo</DialogTitle></DialogHeader>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4 mt-2">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Monto (USD)</Label>
              <Input {...register("monto")} type="number" placeholder="5000" />
              {errors.monto && <p className="text-xs text-destructive">{errors.monto.message}</p>}
            </div>
            <div className="space-y-1">
              <Label>Tipo</Label>
              <Input {...register("tipo")} placeholder="Primer lugar" />
              {errors.tipo && <p className="text-xs text-destructive">{errors.tipo.message}</p>}
            </div>
          </div>
          <div className="space-y-1">
            <Label>Equipo ganador (opcional)</Label>
            <Controller control={control} name="equipoId" render={({ field }) => (
              <Select value={field.value ?? ""} onValueChange={field.onChange}>
                <SelectTrigger><SelectValue placeholder="Sin asignar" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="">Sin asignar</SelectItem>
                  {equiposTorneo?.map((e) => (
                    <SelectItem key={e.equipoId} value={e.equipoId}>{e.nombreEquipo}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )} />
          </div>
          <Button type="submit" className="w-full" disabled={mutation.isPending}>
            {mutation.isPending ? "Asignando…" : "Asignar premio"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// Registrar partida
const partidaSchema = z.object({
  equipoLocalId: z.string().min(1, "Requerido"),
  equipoVisitanteId: z.string().min(1, "Requerido"),
  equipoGanadorId: z.string().min(1, "Requerido"),
  resultado: z.string().min(1, "Requerido"),
  fecha: z.string().min(1, "Requerido"),
}).refine((d) => d.equipoLocalId !== d.equipoVisitanteId, { message: "El local y el visitante deben ser diferentes", path: ["equipoVisitanteId"] });
type PartidaForm = z.infer<typeof partidaSchema>;

function RegistrarPartidaDialog({ torneoId, torneoNombre }: { torneoId: string; torneoNombre: string }) {
  const [open, setOpen] = useState(false);
  const qc = useQueryClient();
  const { data: equiposTorneo } = useQuery({ queryKey: ["torneo", torneoId, "equipos"], queryFn: () => getEquiposPorTorneo(torneoId) });
  const { register, handleSubmit, control, watch, formState: { errors }, reset } = useForm<PartidaForm>({ resolver: zodResolver(partidaSchema) });
  const localId = watch("equipoLocalId");
  const visitanteId = watch("equipoVisitanteId");

  const mutation = useMutation({
    mutationFn: (data: PartidaForm) => {
      const local = equiposTorneo?.find((e) => e.equipoId === data.equipoLocalId);
      const vis = equiposTorneo?.find((e) => e.equipoId === data.equipoVisitanteId);
      return registrarPartida({
        torneoId,
        nombreTorneo: torneoNombre,
        equipoLocalId: data.equipoLocalId,
        nombreLocal: local?.nombreEquipo ?? "",
        equipoVisitanteId: data.equipoVisitanteId,
        nombreVisitante: vis?.nombreEquipo ?? "",
        equipoGanadorId: data.equipoGanadorId,
        resultado: data.resultado,
        fecha: new Date(data.fecha).toISOString(),
      });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["partidas", torneoId] });
      toast.success("Partida registrada. Rankings se actualizarán en breve.");
      setOpen(false);
      reset();
    },
    onError: (e: ApiError) => toast.error(e.detail),
  });

  const equiposFiltrados = equiposTorneo ?? [];
  const posiblesGanadores = equiposFiltrados.filter((e) => e.equipoId === localId || e.equipoId === visitanteId);

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm"><Plus className="h-4 w-4 mr-1" />Registrar partida</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader><DialogTitle>Registrar partida en {torneoNombre}</DialogTitle></DialogHeader>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4 mt-2">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Equipo local</Label>
              <Controller control={control} name="equipoLocalId" render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger><SelectValue placeholder="Local…" /></SelectTrigger>
                  <SelectContent>{equiposFiltrados.map((e) => <SelectItem key={e.equipoId} value={e.equipoId}>{e.nombreEquipo}</SelectItem>)}</SelectContent>
                </Select>
              )} />
              {errors.equipoLocalId && <p className="text-xs text-destructive">{errors.equipoLocalId.message}</p>}
            </div>
            <div className="space-y-1">
              <Label>Equipo visitante</Label>
              <Controller control={control} name="equipoVisitanteId" render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger><SelectValue placeholder="Visitante…" /></SelectTrigger>
                  <SelectContent>{equiposFiltrados.map((e) => <SelectItem key={e.equipoId} value={e.equipoId}>{e.nombreEquipo}</SelectItem>)}</SelectContent>
                </Select>
              )} />
              {errors.equipoVisitanteId && <p className="text-xs text-destructive">{errors.equipoVisitanteId.message}</p>}
            </div>
            <div className="space-y-1">
              <Label>Resultado (ej: 2-1)</Label>
              <Input {...register("resultado")} placeholder="2-1" />
              {errors.resultado && <p className="text-xs text-destructive">{errors.resultado.message}</p>}
            </div>
            <div className="space-y-1">
              <Label>Fecha y hora</Label>
              <Input {...register("fecha")} type="datetime-local" />
              {errors.fecha && <p className="text-xs text-destructive">{errors.fecha.message}</p>}
            </div>
          </div>
          <div className="space-y-1">
            <Label>Equipo ganador</Label>
            <Controller control={control} name="equipoGanadorId" render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange} disabled={posiblesGanadores.length === 0}>
                <SelectTrigger><SelectValue placeholder="Seleccioná local/visitante primero…" /></SelectTrigger>
                <SelectContent>{posiblesGanadores.map((e) => <SelectItem key={e.equipoId} value={e.equipoId}>{e.nombreEquipo}</SelectItem>)}</SelectContent>
              </Select>
            )} />
            {errors.equipoGanadorId && <p className="text-xs text-destructive">{errors.equipoGanadorId.message}</p>}
          </div>
          <Button type="submit" className="w-full" disabled={mutation.isPending}>
            {mutation.isPending ? "Registrando…" : "Registrar partida"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
}

export default function TorneoDetallePage() {
  const { id } = useParams<{ id: string }>();
  const { identidad } = useAuth();
  const esOrg = isOrganizador(identidad);
  const esCapitan = isCapitan(identidad);

  const { data: torneo, isLoading, error } = useQuery({
    queryKey: ["torneo", id],
    queryFn: () => getTorneoPorId(id),
  });

  const { data: equipos } = useQuery({
    queryKey: ["torneo", id, "equipos"],
    queryFn: () => getEquiposPorTorneo(id),
  });

  const { data: partidas, refetch: refetchPartidas } = useQuery({
    queryKey: ["partidas", id],
    queryFn: () => getPartidasPorTorneo(id),
  });

  const { data: premios } = useQuery({
    queryKey: ["torneo", id, "premios"],
    queryFn: () => getPremiosPorTorneo(id),
  });

  if (isLoading) return <Skeleton className="h-64 w-full" />;
  if (error || !torneo) return <ErrorState error={error} />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <div className="flex items-start justify-between gap-4 flex-wrap">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <Trophy className="h-6 w-6 text-warning" />
              <h1 className="text-2xl font-bold text-foreground">{torneo.nombre}</h1>
              <Badge variant="secondary" className="font-mono">{torneo.codigo}</Badge>
            </div>
            <p className="text-sm text-muted-foreground">
              {torneo.nombreVideojuego} · {torneo.nombreOrganizador} · {formatDate(torneo.fechaInicio)}
            </p>
          </div>
          <div className="flex gap-2 flex-wrap">
            {esCapitan && <InscribirEquipoDialog torneoId={id} />}
            {esOrg && (
              <>
                <AsignarPremioDialog torneoId={id} />
                <RegistrarPartidaDialog torneoId={id} torneoNombre={torneo.nombre} />
              </>
            )}
          </div>
        </div>
      </div>

      <Tabs defaultValue="equipos">
        <TabsList>
          <TabsTrigger value="equipos">Equipos ({equipos?.length ?? "…"})</TabsTrigger>
          <TabsTrigger value="partidas">Partidas ({partidas?.length ?? "…"})</TabsTrigger>
          <TabsTrigger value="premios">Premios ({premios?.length ?? "…"})</TabsTrigger>
        </TabsList>

        {/* Q13 */}
        <TabsContent value="equipos">
          {equipos?.length === 0 ? <EmptyState title="Sin equipos inscritos" description="Aún no hay equipos inscriptos en este torneo." /> : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Equipo</TableHead>
                  <TableHead>Fecha inscripción</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {equipos?.map((e) => (
                  <TableRow key={e.equipoId}>
                    <TableCell className="font-medium">{e.nombreEquipo}</TableCell>
                    <TableCell className="text-muted-foreground">{formatDateTime(e.fechaInscripcion)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </TabsContent>

        {/* Q16 */}
        <TabsContent value="partidas">
          <div className="flex justify-end mb-2">
            <Button variant="ghost" size="sm" onClick={() => refetchPartidas()}>
              <RefreshCw className="h-3.5 w-3.5 mr-1" /> Actualizar
            </Button>
          </div>
          {partidas?.length === 0 ? <EmptyState title="Sin partidas" description="Aún no se han registrado partidas en este torneo." /> : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Local</TableHead>
                  <TableHead>Visitante</TableHead>
                  <TableHead>Resultado</TableHead>
                  <TableHead>Fecha</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {partidas?.map((p) => (
                  <TableRow key={p.partidaId}>
                    <TableCell className="font-medium">{p.nombreLocal}</TableCell>
                    <TableCell className="text-muted-foreground">{p.nombreVisitante}</TableCell>
                    <TableCell><Badge variant="secondary">{p.resultado}</Badge></TableCell>
                    <TableCell className="text-muted-foreground">{formatDateTime(p.fecha)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </TabsContent>

        {/* Q20 */}
        <TabsContent value="premios">
          {premios?.length === 0 ? <EmptyState title="Sin premios" description="Aún no se han asignado premios en este torneo." /> : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Tipo</TableHead>
                  <TableHead>Monto</TableHead>
                  <TableHead>Equipo ganador</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {premios?.map((p) => (
                  <TableRow key={p.premioId}>
                    <TableCell><Badge variant="secondary">{p.tipo}</Badge></TableCell>
                    <TableCell className="text-gold font-semibold">{formatCurrency(p.monto)}</TableCell>
                    <TableCell className="text-muted-foreground">{p.nombreEquipo ?? "—"}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </TabsContent>
      </Tabs>
    </div>
  );
}
