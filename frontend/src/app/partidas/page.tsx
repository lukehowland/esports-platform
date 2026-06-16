"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Swords, Calendar, Users } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { ResultadoBadge } from "@/components/resultado-badge";
import { getPartidasPorFecha, getPartidasEntre } from "@/lib/api/partidas";
import { getEquiposPorFecha } from "@/lib/api/equipos";
import { formatDate, formatDateTime } from "@/lib/utils";

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
          <div>
            <p className="text-xs text-muted-foreground mb-3">{data?.length} partidas el {formatDate(fechaBuscada)}</p>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Local</TableHead>
                  <TableHead>Visitante</TableHead>
                  <TableHead>Resultado (marcador)</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.map((p) => (
                  <TableRow key={p.partidaId}>
                    <TableCell className="font-medium">{p.nombreLocal}</TableCell>
                    <TableCell className="text-muted-foreground">{p.nombreVisitante}</TableCell>
                    <TableCell><Badge variant="secondary">{p.resultado}</Badge></TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
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
          <div>
            <p className="text-xs text-muted-foreground mb-3">
              {data?.length} enfrentamiento(s) entre <span className="text-primary">{equipoA?.nombre}</span> y <span className="text-primary">{equipoB?.nombre}</span>
            </p>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Partido</TableHead>
                  <TableHead>Resultado (desde perspectiva del local)</TableHead>
                  <TableHead>Fecha</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.map((p, i) => (
                  <TableRow key={p.partidaId}>
                    <TableCell className="text-muted-foreground">#{i + 1}</TableCell>
                    <TableCell><ResultadoBadge resultado={p.resultado} /></TableCell>
                    <TableCell className="text-muted-foreground">{formatDateTime(p.fecha)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )
      )}
    </div>
  );
}

export default function PartidasPage() {
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
        <Swords className="h-6 w-6 text-success" /> Partidas
      </h1>

      <Tabs defaultValue="fecha">
        <TabsList>
          <TabsTrigger value="fecha"><Calendar className="h-3.5 w-3.5 mr-1" /> Por fecha (Q18)</TabsTrigger>
          <TabsTrigger value="h2h"><Users className="h-3.5 w-3.5 mr-1" /> Cara a cara (Q19)</TabsTrigger>
        </TabsList>
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
