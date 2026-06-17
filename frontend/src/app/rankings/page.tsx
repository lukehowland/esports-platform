"use client";

import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { BarChart3, Trophy, Swords, User, RefreshCw, Loader2 } from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { RankingPosition } from "@/components/ranking-position";
import { getRankingEquipos, getRankingVictorias, getRankingJugadores } from "@/lib/api/ranking";
import { getEquipoPorId } from "@/lib/api/equipos";
import { useQuery as useQ } from "@tanstack/react-query";
import { shortId } from "@/lib/utils";

const TOP_OPTIONS = [5, 10, 20];

function EquipoNombre({ equipoId }: { equipoId: string }) {
  const { data, isLoading } = useQ({
    queryKey: ["equipo", equipoId],
    queryFn: () => getEquipoPorId(equipoId),
    staleTime: 5 * 60_000,
  });
  if (isLoading) return <span className="text-muted-foreground animate-pulse">…</span>;
  if (!data) return <span className="font-mono text-xs text-muted-foreground">{shortId(equipoId)}</span>;
  return <span className="font-medium">[{data.tag}] {data.nombre}</span>;
}

function RankingEquiposTab() {
  const [top, setTop] = useState(10);
  const qc = useQueryClient();
  const { data, isLoading, error, isFetching, refetch } = useQuery({
    queryKey: ["ranking", "equipos", top],
    queryFn: () => getRankingEquipos(top),
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <p className="text-xs text-muted-foreground">Top equipos por torneos disputados</p>
        <div className="flex items-center gap-2">
          <div className="flex gap-1">
            {TOP_OPTIONS.map((n) => (
              <button key={n} onClick={() => setTop(n)}
                className={`rounded px-2 py-0.5 text-xs transition-colors ${top === n ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground"}`}
              >Top {n}</button>
            ))}
          </div>
          <Button variant="ghost" size="icon" onClick={() => refetch()} disabled={isFetching} title="Actualizar">
            {isFetching ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <RefreshCw className="h-3.5 w-3.5" />}
          </Button>
        </div>
      </div>
      <p className="text-xs text-muted-foreground italic">Rankings con consistencia eventual — se actualizan tras inscripciones de equipos.</p>
      {isLoading ? <Skeleton className="h-48" /> :
       error ? <ErrorState error={error} onRetry={refetch} /> :
       data?.length === 0 ? <EmptyState title="Sin datos" description="Inscribí equipos en torneos para ver el ranking." /> : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-12">#</TableHead>
              <TableHead>Equipo</TableHead>
              <TableHead className="text-right">Torneos</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data?.map((r, i) => (
              <TableRow key={r.equipoId}>
                <TableCell><RankingPosition position={i + 1} /></TableCell>
                <TableCell><EquipoNombre equipoId={r.equipoId} /></TableCell>
                <TableCell className="text-right font-mono font-semibold text-primary">{r.totalTorneos.toString()}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}

function RankingVictoriasTab() {
  const [top, setTop] = useState(10);
  const { data, isLoading, error, isFetching, refetch } = useQuery({
    queryKey: ["ranking", "victorias", top],
    queryFn: () => getRankingVictorias(top),
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <p className="text-xs text-muted-foreground">Top equipos por victorias totales</p>
        <div className="flex items-center gap-2">
          <div className="flex gap-1">
            {TOP_OPTIONS.map((n) => (
              <button key={n} onClick={() => setTop(n)}
                className={`rounded px-2 py-0.5 text-xs transition-colors ${top === n ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground"}`}
              >Top {n}</button>
            ))}
          </div>
          <Button variant="ghost" size="icon" onClick={() => refetch()} disabled={isFetching}>
            {isFetching ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <RefreshCw className="h-3.5 w-3.5" />}
          </Button>
        </div>
      </div>
      <p className="text-xs text-muted-foreground italic">Ranking actualizado tras cada partida registrada (consistencia eventual).</p>
      {isLoading ? <Skeleton className="h-48" /> :
       error ? <ErrorState error={error} onRetry={refetch} /> :
       data?.length === 0 ? <EmptyState title="Sin datos" description="Registrá partidas para ver victorias." /> : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-12">#</TableHead>
              <TableHead>Equipo</TableHead>
              <TableHead className="text-right">Victorias</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data?.map((r, i) => (
              <TableRow key={r.equipoId}>
                <TableCell><RankingPosition position={i + 1} /></TableCell>
                <TableCell><EquipoNombre equipoId={r.equipoId} /></TableCell>
                <TableCell className="text-right font-mono font-semibold text-success">{r.totalVictorias.toString()}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}

function RankingJugadoresTab() {
  const [top, setTop] = useState(10);
  const { data, isLoading, error, isFetching, refetch } = useQuery({
    queryKey: ["ranking", "jugadores", top],
    queryFn: () => getRankingJugadores(top),
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <p className="text-xs text-muted-foreground">Jugadores más activos por torneos disputados</p>
        <div className="flex items-center gap-2">
          <div className="flex gap-1">
            {TOP_OPTIONS.map((n) => (
              <button key={n} onClick={() => setTop(n)}
                className={`rounded px-2 py-0.5 text-xs transition-colors ${top === n ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground"}`}
              >Top {n}</button>
            ))}
          </div>
          <Button variant="ghost" size="icon" onClick={() => refetch()} disabled={isFetching}>
            {isFetching ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <RefreshCw className="h-3.5 w-3.5" />}
          </Button>
        </div>
      </div>
      <p className="text-xs text-muted-foreground italic">Los IDs se resuelven como nombres desde el roster de cada equipo registrado.</p>
      {isLoading ? <Skeleton className="h-48" /> :
       error ? <ErrorState error={error} onRetry={refetch} /> :
       data?.length === 0 ? <EmptyState title="Sin datos" description="Inscribí equipos con jugadores para ver el ranking." /> : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-12">#</TableHead>
              <TableHead>Jugador</TableHead>
              <TableHead className="text-right">Torneos</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data?.map((r, i) => (
              <TableRow key={r.jugadorId}>
                <TableCell><RankingPosition position={i + 1} /></TableCell>
                <TableCell>
                  {r.nombreJugador
                    ? <span className="font-medium">{r.nombreJugador}</span>
                    : <span className="font-mono text-xs text-muted-foreground" title={r.jugadorId}>{shortId(r.jugadorId)}</span>
                  }
                </TableCell>
                <TableCell className="text-right font-mono font-semibold text-primary">{r.totalTorneos.toString()}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}

export default function RankingsPage() {
  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ estadísticas</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <BarChart3 className="w-7 h-7 text-gold" /> Rankings
        </h1>
      </div>

      <Tabs defaultValue="equipos">
        <TabsList>
          <TabsTrigger value="equipos"><Trophy className="h-3.5 w-3.5 mr-1" /> Equipos (Q7)</TabsTrigger>
          <TabsTrigger value="victorias"><Swords className="h-3.5 w-3.5 mr-1" /> Victorias (Q22)</TabsTrigger>
          <TabsTrigger value="jugadores"><User className="h-3.5 w-3.5 mr-1" /> Jugadores (Q23)</TabsTrigger>
        </TabsList>
        <TabsContent value="equipos"><RankingEquiposTab /></TabsContent>
        <TabsContent value="victorias"><RankingVictoriasTab /></TabsContent>
        <TabsContent value="jugadores"><RankingJugadoresTab /></TabsContent>
      </Tabs>
    </div>
  );
}
