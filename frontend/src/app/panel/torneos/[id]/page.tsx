"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Trophy, Plus, RefreshCw, ArrowLeft, Lock, Pencil, Trash2, Loader2, CalendarRange } from "lucide-react";
import { toast } from "sonner";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogDescription, DialogFooter } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { RequireRole } from "@/lib/auth/require-role";
import { useAuth } from "@/lib/auth/context";
import { isAdmin, isOrganizador } from "@/lib/auth/types";
import {
  getTorneoPorId, getEquiposPorTorneo, getPremiosPorTorneo,
  inscribirEquipo, asignarPremio, editarTorneo, eliminarTorneo,
  type TorneoResponse,
} from "@/lib/api/torneos";
import { getEquiposPorFecha } from "@/lib/api/equipos";
import { getPartidasPorTorneo, registrarPartida } from "@/lib/api/partidas";
import { formatDateTime, formatDate, formatCurrency } from "@/lib/utils";
import type { ApiError } from "@/lib/api/fetcher";

export default function PanelTorneoPage() {
  return (
    <RequireRole roles={["admin", "organizador"]}>
      <TorneoGestion />
    </RequireRole>
  );
}

function TorneoGestion() {
  const { id } = useParams<{ id: string }>();
  const { identidad } = useAuth();

  const { data: torneo, isLoading, error } = useQuery({
    queryKey: ["torneo", id],
    queryFn: () => getTorneoPorId(id),
  });

  if (isLoading) return <Skeleton className="h-64 w-full" />;
  if (error || !torneo) return <ErrorState error={error} />;

  // Ownership: el admin gestiona cualquier torneo; el organizador, solo los suyos.
  const esDueño =
    isAdmin(identidad) ||
    (isOrganizador(identidad) && identidad.organizadorId === torneo.organizadorId);

  return (
    <div className="space-y-6">
      <Link href={isAdmin(identidad) ? "/panel/torneos" : "/panel/mis-torneos"} className="inline-flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors">
        <ArrowLeft className="w-3.5 h-3.5" /> Volver a torneos
      </Link>

      <div className="flex flex-col items-start justify-between gap-3 sm:flex-row">
        <div>
          <div className="mb-1 flex flex-wrap items-center gap-2">
            <Trophy className="h-6 w-6 text-warning" />
            <h1 className="text-2xl font-bold text-foreground">{torneo.nombre}</h1>
            <Badge variant="secondary" className="font-mono">{torneo.codigo}</Badge>
          </div>
          <p className="text-sm text-muted-foreground">
            {torneo.nombreVideojuego} · {torneo.nombreOrganizador}
          </p>
          <p className="text-xs text-muted-foreground mt-1 flex items-center gap-1">
            <CalendarRange className="w-3.5 h-3.5" /> {formatDate(torneo.fechaInicio)} → {formatDate(torneo.fechaFin)}
          </p>
        </div>
        {esDueño && <TorneoActions torneo={torneo} />}
      </div>

      {esDueño ? (
        <GestionContent torneoId={id} torneoNombre={torneo.nombre} esAdmin={isAdmin(identidad)} />
      ) : (
        <HudPanel className="p-6 flex items-start gap-3">
          <Lock className="w-5 h-5 text-muted-foreground shrink-0 mt-0.5" />
          <div>
            <p className="font-semibold text-foreground">Este torneo no es tuyo</p>
            <p className="text-sm text-muted-foreground mt-1">
              Solo el organizador dueño del torneo (o un administrador) puede gestionar premios y partidas.
              Podés ver el detalle público en{" "}
              <Link href={`/torneos/${id}`} className="text-primary underline">la página del torneo</Link>.
            </p>
          </div>
        </HudPanel>
      )}
    </div>
  );
}

function GestionContent({ torneoId, torneoNombre, esAdmin }: { torneoId: string; torneoNombre: string; esAdmin: boolean }) {
  const { data: equipos } = useQuery({
    queryKey: ["torneo", torneoId, "equipos"],
    queryFn: () => getEquiposPorTorneo(torneoId),
  });

  const { data: partidas, refetch: refetchPartidas } = useQuery({
    queryKey: ["partidas", torneoId],
    queryFn: () => getPartidasPorTorneo(torneoId),
  });

  const { data: premios } = useQuery({
    queryKey: ["torneo", torneoId, "premios"],
    queryFn: () => getPremiosPorTorneo(torneoId),
  });

  return (
    <>
      <div className="flex gap-2 flex-wrap">
        {esAdmin && <InscribirEquipoDialog torneoId={torneoId} />}
        <AsignarPremioDialog torneoId={torneoId} />
        <RegistrarPartidaDialog torneoId={torneoId} torneoNombre={torneoNombre} />
      </div>

      <Tabs defaultValue="equipos">
        <TabsList>
          <TabsTrigger value="equipos">Inscritos ({equipos?.length ?? "…"})</TabsTrigger>
          <TabsTrigger value="partidas">Partidas ({partidas?.length ?? "…"})</TabsTrigger>
          <TabsTrigger value="premios">Premios ({premios?.length ?? "…"})</TabsTrigger>
        </TabsList>

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
    </>
  );
}

// RF-06: editar (nombre/fecha fin) y eliminar torneo. Bloqueado si tiene inscritos/premios (409).
function TorneoActions({ torneo }: { torneo: TorneoResponse }) {
  const router = useRouter();
  const qc = useQueryClient();
  const [editOpen, setEditOpen] = useState(false);
  const [delOpen, setDelOpen] = useState(false);
  const [nombre, setNombre] = useState(torneo.nombre);
  const [fechaFin, setFechaFin] = useState(new Date(torneo.fechaFin).toISOString().slice(0, 16));

  const abrirEdicion = () => {
    setNombre(torneo.nombre);
    setFechaFin(new Date(torneo.fechaFin).toISOString().slice(0, 16));
    setEditOpen(true);
  };

  const editar = useMutation({
    mutationFn: () => editarTorneo(torneo.torneoId, { nombre: nombre.trim(), fechaFin: new Date(fechaFin).toISOString() }),
    onSuccess: () => {
      toast.success("Torneo actualizado");
      qc.invalidateQueries({ queryKey: ["torneo", torneo.torneoId] });
      qc.invalidateQueries({ queryKey: ["torneos"] });
      setEditOpen(false);
    },
    onError: (e) => toast.error(e instanceof Error ? (e as ApiError).detail ?? e.message : "No se pudo editar"),
  });

  const eliminar = useMutation({
    mutationFn: () => eliminarTorneo(torneo.torneoId),
    onSuccess: () => {
      toast.success("Torneo eliminado");
      qc.invalidateQueries({ queryKey: ["torneos"] });
      router.push("/panel/torneos");
    },
    onError: (e) => toast.error(e instanceof Error ? (e as ApiError).detail ?? e.message : "No se pudo eliminar"),
  });

  return (
    <div className="flex items-center gap-1 shrink-0">
      <Button size="sm" variant="outline" onClick={abrirEdicion}><Pencil className="w-3.5 h-3.5" /> Editar</Button>
      <Button size="sm" variant="outline" onClick={() => setDelOpen(true)}><Trash2 className="w-3.5 h-3.5 text-destructive" /> Eliminar</Button>

      <Dialog open={editOpen} onOpenChange={setEditOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Editar torneo</DialogTitle>
            <DialogDescription>No se puede editar si tiene equipos inscritos o premios asignados.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-1.5">
              <Label className="eyebrow">Nombre</Label>
              <Input value={nombre} onChange={(e) => setNombre(e.target.value)} />
            </div>
            <div className="space-y-1.5">
              <Label className="eyebrow">Fecha de fin</Label>
              <Input type="datetime-local" value={fechaFin} onChange={(e) => setFechaFin(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setEditOpen(false)}>Cancelar</Button>
            <Button disabled={editar.isPending} onClick={() => editar.mutate()}>
              {editar.isPending && <Loader2 className="w-4 h-4 animate-spin" />} Guardar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={delOpen} onOpenChange={setDelOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Eliminar torneo</DialogTitle>
            <DialogDescription>
              Vas a eliminar <span className="font-semibold text-foreground">{torneo.nombre}</span>.
              No se puede eliminar si tiene equipos inscritos o premios asignados.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDelOpen(false)}>Cancelar</Button>
            <Button variant="destructive" disabled={eliminar.isPending} onClick={() => eliminar.mutate()}>
              {eliminar.isPending && <Loader2 className="w-4 h-4 animate-spin" />} Eliminar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

// Inscribir equipo — solo admin (superusuario). El organizador no inscribe: lo hace el capitán.
function InscribirEquipoDialog({ torneoId }: { torneoId: string }) {
  const [open, setOpen] = useState(false);
  const [equipoId, setEquipoId] = useState("");
  const qc = useQueryClient();
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
            <Select value={equipoId} onValueChange={setEquipoId}>
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
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
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
              <Select value={field.value ?? "__none__"} onValueChange={(v) => field.onChange(v === "__none__" ? undefined : v)}>
                <SelectTrigger><SelectValue placeholder="Sin asignar" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="__none__">Sin asignar</SelectItem>
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
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
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
