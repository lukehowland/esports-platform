"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { RefreshCw } from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { ResultadoBadge } from "@/components/resultado-badge";
import { getEquipoPorId, getIntegrantesPorEquipo, getJugadoresPorEquipo } from "@/lib/api/equipos";
import { getTorneosPorEquipo, getPremiosPorEquipo } from "@/lib/api/torneos";
import { getPartidasPorEquipo } from "@/lib/api/partidas";
import { getStatsEquipoTorneo } from "@/lib/api/ranking";
import { formatDate, formatDateTime, formatCurrency } from "@/lib/utils";

function StatsTab({ equipoId }: { equipoId: string }) {
  const [torneoIdInput, setTorneoIdInput] = useState("");
  const [torneoIdQuery, setTorneoIdQuery] = useState("");

  const { data: torneos } = useQuery({
    queryKey: ["torneo", "por-equipo", equipoId],
    queryFn: () => getTorneosPorEquipo(equipoId),
  });

  const { data: stats, isLoading, refetch } = useQuery({
    queryKey: ["stats", equipoId, torneoIdQuery],
    queryFn: () => getStatsEquipoTorneo(equipoId, torneoIdQuery),
    enabled: !!torneoIdQuery,
  });

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label className="text-sm">Seleccionar torneo</Label>
        <div className="flex gap-2">
          <Select value={torneoIdInput} onValueChange={setTorneoIdInput}>
            <SelectTrigger className="flex-1">
              <SelectValue placeholder="Elegí un torneo…" />
            </SelectTrigger>
            <SelectContent>
              {torneos?.map((t) => (
                <SelectItem key={t.torneoId} value={t.torneoId}>{t.nombreTorneo}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button
            variant="outline"
            onClick={() => { setTorneoIdQuery(torneoIdInput); }}
            disabled={!torneoIdInput}
          >
            Ver stats
          </Button>
        </div>
      </div>

      {torneoIdQuery && (
        isLoading ? (
          <Skeleton className="h-32" />
        ) : stats ? (
          <div className="grid grid-cols-3 gap-4">
            {[
              { label: "Partidas jugadas", value: stats.partidasJugadas, color: "text-foreground" },
              { label: "Victorias", value: stats.victorias, color: "text-success" },
              { label: "Derrotas", value: stats.derrotas, color: "text-destructive" },
            ].map(({ label, value, color }) => (
              <Card key={label}>
                <CardContent className="pt-4 text-center">
                  <div className={`text-3xl font-bold ${color}`}>{value.toString()}</div>
                  <div className="text-xs text-muted-foreground mt-1">{label}</div>
                </CardContent>
              </Card>
            ))}
            <div className="col-span-3 flex items-center gap-1 text-xs text-muted-foreground">
              <RefreshCw className="h-3 w-3" />
              Stats consistencia eventual — <button className="underline text-primary" onClick={() => refetch()}>actualizar</button>
            </div>
          </div>
        ) : (
          <EmptyState title="Sin estadísticas" description="El equipo aún no jugó partidas en este torneo." />
        )
      )}
    </div>
  );
}

export default function EquipoDetallePage() {
  const { id } = useParams<{ id: string }>();

  const [paisFiltro, setPaisFiltro] = useState("");
  const [paisAplicado, setPaisAplicado] = useState("");

  const { data: equipo, isLoading: loadingEquipo, error } = useQuery({
    queryKey: ["equipo", id],
    queryFn: () => getEquipoPorId(id),
  });

  const { data: integrantes, isLoading: loadingIntegrantes } = useQuery({
    queryKey: ["equipo", id, "integrantes"],
    queryFn: () => getIntegrantesPorEquipo(id),
  });

  const { data: jugadoresFiltrados, isLoading: loadingFiltro } = useQuery({
    queryKey: ["equipo", id, "jugadores", paisAplicado],
    queryFn: () => getJugadoresPorEquipo(id, paisAplicado || undefined),
    enabled: !!paisAplicado,
  });

  const { data: torneos } = useQuery({
    queryKey: ["torneo", "por-equipo", id],
    queryFn: () => getTorneosPorEquipo(id),
  });

  const { data: partidas } = useQuery({
    queryKey: ["partidas", "por-equipo", id],
    queryFn: () => getPartidasPorEquipo(id),
  });

  const { data: premios } = useQuery({
    queryKey: ["premios", "por-equipo", id],
    queryFn: () => getPremiosPorEquipo(id),
  });

  if (loadingEquipo) return <Skeleton className="h-64 w-full" />;
  if (error || !equipo) return <ErrorState error={error} />;

  return (
    <div className="space-y-6">
      {/* Header del equipo */}
      <div>
        <div className="flex items-center gap-3 mb-1">
          <Badge variant="secondary" className="font-mono text-primary text-sm px-3 py-1">{equipo.tag}</Badge>
          <h1 className="text-2xl font-bold text-foreground">{equipo.nombre}</h1>
        </div>
        <p className="text-sm text-muted-foreground">{equipo.pais} · Creado {formatDate(equipo.fechaCreacion)}</p>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="integrantes">
        <TabsList className="flex-wrap h-auto gap-1">
          <TabsTrigger value="integrantes">Integrantes ({integrantes?.length ?? "…"})</TabsTrigger>
          <TabsTrigger value="filtrar">Filtrar por país (Q3)</TabsTrigger>
          <TabsTrigger value="torneos">Torneos ({torneos?.length ?? "…"})</TabsTrigger>
          <TabsTrigger value="partidas">Partidas ({partidas?.length ?? "…"})</TabsTrigger>
          <TabsTrigger value="premios">Premios ({premios?.length ?? "…"})</TabsTrigger>
          <TabsTrigger value="stats">Estadísticas</TabsTrigger>
        </TabsList>

        {/* Q6 — Integrantes */}
        <TabsContent value="integrantes">
          {loadingIntegrantes ? <Skeleton className="h-48" /> : integrantes?.length === 0 ? (
            <EmptyState title="Sin integrantes" description="Aún no se han agregado jugadores." />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nickname</TableHead>
                  <TableHead>Nombre</TableHead>
                  <TableHead>País</TableHead>
                  <TableHead>Rol</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {integrantes?.map((j) => (
                  <TableRow key={j.jugadorId}>
                    <TableCell className="font-mono text-primary">{j.nickname}</TableCell>
                    <TableCell>{j.nombre}</TableCell>
                    <TableCell>{j.pais}</TableCell>
                    <TableCell><Badge variant="muted">{j.rol}</Badge></TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </TabsContent>

        {/* Q3 — Filtrar por país */}
        <TabsContent value="filtrar">
          <div className="space-y-4">
            <div className="flex gap-2 max-w-sm">
              <Input placeholder="País (ej: Bolivia)" value={paisFiltro} onChange={(e) => setPaisFiltro(e.target.value)} onKeyDown={(e) => e.key === "Enter" && setPaisAplicado(paisFiltro)} />
              <Button variant="outline" onClick={() => setPaisAplicado(paisFiltro)}>Filtrar</Button>
              {paisAplicado && <Button variant="ghost" onClick={() => { setPaisFiltro(""); setPaisAplicado(""); }}>Limpiar</Button>}
            </div>
            {paisAplicado && (
              loadingFiltro ? <Skeleton className="h-32" /> :
              jugadoresFiltrados?.length === 0 ? <EmptyState title="Sin jugadores" description={`No hay jugadores de ${paisAplicado} en este equipo.`} /> :
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Nickname</TableHead>
                    <TableHead>Nombre</TableHead>
                    <TableHead>Rol</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {jugadoresFiltrados?.map((j) => (
                    <TableRow key={j.jugadorId}>
                      <TableCell className="font-mono text-primary">{j.nickname}</TableCell>
                      <TableCell>{j.nombre}</TableCell>
                      <TableCell><Badge variant="muted">{j.rol}</Badge></TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </div>
        </TabsContent>

        {/* Q14 — Torneos del equipo */}
        <TabsContent value="torneos">
          {torneos?.length === 0 ? <EmptyState title="Sin torneos" description="Este equipo no ha participado en torneos." /> : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Torneo</TableHead>
                  <TableHead>Videojuego</TableHead>
                  <TableHead>Fecha</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {torneos?.map((t) => (
                  <TableRow key={t.torneoId}>
                    <TableCell className="font-medium">{t.nombreTorneo}</TableCell>
                    <TableCell className="text-muted-foreground">{t.nombreVideojuego}</TableCell>
                    <TableCell className="text-muted-foreground">{formatDate(t.fechaInicio)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </TabsContent>

        {/* Q17 — Partidas del equipo */}
        <TabsContent value="partidas">
          {partidas?.length === 0 ? <EmptyState title="Sin partidas" description="Este equipo no ha jugado partidas." /> : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Torneo</TableHead>
                  <TableHead>Rival</TableHead>
                  <TableHead>Resultado</TableHead>
                  <TableHead>Fecha</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {partidas?.map((p) => (
                  <TableRow key={p.partidaId}>
                    <TableCell className="text-muted-foreground text-sm">{p.nombreTorneo}</TableCell>
                    <TableCell className="font-medium">{p.rival}</TableCell>
                    <TableCell><ResultadoBadge resultado={p.resultado} /></TableCell>
                    <TableCell className="text-muted-foreground">{formatDateTime(p.fecha)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </TabsContent>

        {/* Q21 — Premios del equipo */}
        <TabsContent value="premios">
          {premios?.length === 0 ? <EmptyState title="Sin premios" description="Este equipo aún no ha ganado premios." /> : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Torneo</TableHead>
                  <TableHead>Tipo</TableHead>
                  <TableHead>Monto</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {premios?.map((p) => (
                  <TableRow key={p.premioId}>
                    <TableCell className="font-medium">{p.nombreTorneo}</TableCell>
                    <TableCell><Badge variant="secondary">{p.tipo}</Badge></TableCell>
                    <TableCell className="text-gold font-semibold">{formatCurrency(p.monto)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </TabsContent>

        {/* Q24 — Stats por torneo */}
        <TabsContent value="stats">
          <StatsTab equipoId={id} />
        </TabsContent>
      </Tabs>
    </div>
  );
}
