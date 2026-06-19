"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Trophy, RefreshCw, CalendarRange } from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { getTorneoPorId, getEquiposPorTorneo, getPremiosPorTorneo } from "@/lib/api/torneos";
import { getPartidasPorTorneo } from "@/lib/api/partidas";
import { formatDate, formatDateTime, formatCurrency } from "@/lib/utils";

export default function TorneoDetallePage() {
  const { id } = useParams<{ id: string }>();

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
        <div className="flex items-center gap-2 mb-1">
          <Trophy className="h-6 w-6 text-warning" />
          <h1 className="text-2xl font-bold text-foreground">{torneo.nombre}</h1>
          <Badge variant="secondary" className="font-mono">{torneo.codigo}</Badge>
        </div>
        <p className="text-sm text-muted-foreground">
          {torneo.nombreVideojuego} · {torneo.nombreOrganizador}
        </p>
        <p className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
          <CalendarRange className="h-3.5 w-3.5" />
          {formatDate(torneo.fechaInicio)} → {formatDate(torneo.fechaFin)}
        </p>
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
