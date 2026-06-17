"use client";

import { useState } from "react";
import { useQueries, useQuery } from "@tanstack/react-query";
import { Swords, Calendar, Users, Radio } from "lucide-react";
import { Button } from "@/components/ui/button";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { ResultadoBadge } from "@/components/resultado-badge";
import { getPartidasPorFecha, getPartidasEntre, getPartidasPorTorneo } from "@/lib/api/partidas";
import { getEquiposPorFecha } from "@/lib/api/equipos";
import { getTorneosPorFecha } from "@/lib/api/torneos";
import { formatDate, formatDateTime } from "@/lib/utils";

// Query-first: no hay "listar todas las partidas". Las más recientes se componen
// abriendo cada torneo (Q12) y juntando sus partidas (Q16), ordenadas por fecha desc.
function PartidasRecientes() {
  const { data: torneos, isLoading: loadingTorneos, error, refetch } = useQuery({
    queryKey: ["torneos", "por-fecha"],
    queryFn: getTorneosPorFecha,
  });

  const { partidas, cargandoPartidas } = useQueries({
    queries: (torneos ?? []).map((t) => ({
      queryKey: ["partidas", "por-torneo", t.torneoId],
      queryFn: () => getPartidasPorTorneo(t.torneoId),
      enabled: torneos !== undefined,
    })),
    combine: (results) => {
      const partidas = results.flatMap((r, i) =>
        (r.data ?? []).map((p) => ({ ...p, nombreTorneo: torneos?.[i]?.nombreTorneo ?? "" }))
      );
      partidas.sort((a, b) => new Date(b.fecha).getTime() - new Date(a.fecha).getTime());
      return { partidas, cargandoPartidas: results.some((r) => r.isLoading) };
    },
  });

  const cargando = loadingTorneos || cargandoPartidas;
  const recientes = partidas.slice(0, 40);

  if (error) return <ErrorState error={error} onRetry={refetch} />;

  if (cargando) return <Skeleton className="h-64" />;

  if (recientes.length === 0)
    return <EmptyState title="Sin partidas" description="Todavía no hay partidas registradas." />;

  return (
    <HudPanel>
      <div className="px-4 py-2 border-b border-line flex items-center gap-2">
        <Radio className="h-3.5 w-3.5 text-lime" />
        <HudEyebrow>últimas {recientes.length} partidas</HudEyebrow>
      </div>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Torneo</TableHead>
            <TableHead>Local</TableHead>
            <TableHead>Visitante</TableHead>
            <TableHead>Resultado</TableHead>
            <TableHead>Fecha</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {recientes.map((p) => (
            <TableRow key={p.partidaId}>
              <TableCell className="text-muted-foreground text-sm">{p.nombreTorneo}</TableCell>
              <TableCell className="font-medium">{p.nombreLocal}</TableCell>
              <TableCell className="text-muted-foreground">{p.nombreVisitante}</TableCell>
              <TableCell><ResultadoBadge resultado={p.resultado} /></TableCell>
              <TableCell className="text-muted-foreground">{formatDateTime(p.fecha)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </HudPanel>
  );
}

function PartidaPorFecha() {
  const [fecha, setFecha] = useState("");
  const [fechaBuscada, setFechaBuscada] = useState("");

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["partidas", "fecha", fechaBuscada],
    queryFn: () => getPartidasPorFecha(fechaBuscada),
    enabled: !!fechaBuscada,
  });

  return (
    <div className="space-y-4">
      <div className="flex gap-2 max-w-xs">
        <Input
          type="date"
          value={fecha}
          onChange={(e) => setFecha(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && setFechaBuscada(fecha)}
        />
        <Button variant="outline" onClick={() => setFechaBuscada(fecha)} disabled={!fecha}>
          Buscar
        </Button>
      </div>
      {fechaBuscada && (
        isLoading ? <Skeleton className="h-32" /> :
        error ? <ErrorState error={error} onRetry={refetch} /> :
        data?.length === 0 ? <EmptyState title="Sin partidas" description={`No hubo partidas el ${formatDate(fechaBuscada)}.`} /> : (
          <HudPanel>
              <div className="px-4 py-2 border-b border-line">
                <HudEyebrow>{data?.length} partidas el {formatDate(fechaBuscada)}</HudEyebrow>
              </div>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Local</TableHead>
                    <TableHead>Visitante</TableHead>
                    <TableHead>Resultado</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data?.map((p) => (
                    <TableRow key={p.partidaId}>
                      <TableCell className="font-medium">{p.nombreLocal}</TableCell>
                      <TableCell className="text-muted-foreground">{p.nombreVisitante}</TableCell>
                      <TableCell>
                        <span className="hud-clip-sm border border-violet/30 bg-violet/10 text-violet font-mono text-xs px-2 py-0.5">
                          {p.resultado}
                        </span>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </HudPanel>
        )
      )}
    </div>
  );
}

function PartidaEntreDosEquipos() {
  const [equipoAId, setEquipoAId] = useState("");
  const [equipoBId, setEquipoBId] = useState("");
  const [buscar, setBuscar] = useState(false);

  const { data: equipos, isLoading: loadingEquipos } = useQuery({
    queryKey: ["equipos", "por-fecha"],
    queryFn: getEquiposPorFecha,
  });

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["partidas", "entre", equipoAId, equipoBId],
    queryFn: () => getPartidasEntre(equipoAId, equipoBId),
    enabled: buscar && !!equipoAId && !!equipoBId,
  });

  const handleBuscar = () => {
    if (equipoAId && equipoBId && equipoAId !== equipoBId) setBuscar(true);
  };

  const equipoA = equipos?.find((e) => e.equipoId === equipoAId);
  const equipoB = equipos?.find((e) => e.equipoId === equipoBId);

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 max-w-lg">
        <div className="space-y-1">
          <Label>Equipo A</Label>
          <Select value={equipoAId} onValueChange={(v) => { setEquipoAId(v); setBuscar(false); }}>
            <SelectTrigger disabled={loadingEquipos}>
              <SelectValue placeholder="Seleccionar equipo…" />
            </SelectTrigger>
            <SelectContent>
              {equipos?.filter((e) => e.equipoId !== equipoBId).map((e) => (
                <SelectItem key={e.equipoId} value={e.equipoId}>[{e.tag}] {e.nombre}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1">
          <Label>Equipo B</Label>
          <Select value={equipoBId} onValueChange={(v) => { setEquipoBId(v); setBuscar(false); }}>
            <SelectTrigger disabled={loadingEquipos}>
              <SelectValue placeholder="Seleccionar equipo…" />
            </SelectTrigger>
            <SelectContent>
              {equipos?.filter((e) => e.equipoId !== equipoAId).map((e) => (
                <SelectItem key={e.equipoId} value={e.equipoId}>[{e.tag}] {e.nombre}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>
      <Button onClick={handleBuscar} disabled={!equipoAId || !equipoBId || equipoAId === equipoBId}>
        Ver enfrentamientos cara a cara
      </Button>

      {buscar && equipoAId && equipoBId && (
        isLoading ? <Skeleton className="h-32" /> :
        error ? <ErrorState error={error} onRetry={refetch} /> :
        data?.length === 0 ? (
          <EmptyState
            title="Sin enfrentamientos"
            description={`${equipoA?.nombre ?? "Equipo A"} y ${equipoB?.nombre ?? "Equipo B"} nunca se han enfrentado.`}
          />
        ) : (
          <HudPanel>
            <div className="px-4 py-2 border-b border-line">
              <HudEyebrow>{data?.length} enfrentamiento(s) · {equipoA?.nombre} vs {equipoB?.nombre}</HudEyebrow>
            </div>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Partido</TableHead>
                  <TableHead>Resultado</TableHead>
                  <TableHead>Fecha</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.map((p, i) => (
                  <TableRow key={p.partidaId}>
                    <TableCell className="text-muted-foreground font-mono">#{i + 1}</TableCell>
                    <TableCell><ResultadoBadge resultado={p.resultado} /></TableCell>
                    <TableCell className="text-muted-foreground">{formatDateTime(p.fecha)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </HudPanel>
        )
      )}
    </div>
  );
}

export default function PartidasPage() {
  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ resultados</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <Swords className="w-7 h-7 text-violet" /> Partidas
        </h1>
      </div>

      <Tabs defaultValue="recientes">
        <TabsList>
          <TabsTrigger value="recientes"><Radio className="h-3.5 w-3.5 mr-1" /> Recientes</TabsTrigger>
          <TabsTrigger value="fecha"><Calendar className="h-3.5 w-3.5 mr-1" /> Por fecha (Q18)</TabsTrigger>
          <TabsTrigger value="h2h"><Users className="h-3.5 w-3.5 mr-1" /> Cara a cara (Q19)</TabsTrigger>
        </TabsList>
        <TabsContent value="recientes">
          <PartidasRecientes />
        </TabsContent>
        <TabsContent value="fecha">
          <PartidaPorFecha />
        </TabsContent>
        <TabsContent value="h2h">
          <PartidaEntreDosEquipos />
        </TabsContent>
      </Tabs>
    </div>
  );
}
