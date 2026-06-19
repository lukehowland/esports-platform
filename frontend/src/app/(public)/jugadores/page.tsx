"use client";

import { useMemo, useState } from "react";
import { useQueries, useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { Search, User, Globe } from "lucide-react";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import {
  getEquiposPorFecha,
  getIntegrantesPorEquipo,
  getJugadorPorNickname,
  getJugadoresPorPais,
  type JugadorResponse,
} from "@/lib/api/equipos";
import { ApiError } from "@/lib/api/fetcher";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

// El backend es query-first: no hay un endpoint "listar todos los jugadores".
// La vista "Todos" se compone en el cliente abriendo cada equipo (Q4) y juntando
// sus integrantes (Q6), luego filtra en memoria por nickname/nombre/país/rol.
function TodosJugadores() {
  const [busqueda, setBusqueda] = useState("");
  const [rolFiltro, setRolFiltro] = useState("");

  const { data: equipos, isLoading: loadingEquipos, error, refetch } = useQuery({
    queryKey: ["equipos", "por-fecha"],
    queryFn: getEquiposPorFecha,
  });

  const { jugadores, cargandoIntegrantes } = useQueries({
    queries: (equipos ?? []).map((e) => ({
      queryKey: ["equipo", e.equipoId, "integrantes"],
      queryFn: () => getIntegrantesPorEquipo(e.equipoId),
      enabled: equipos !== undefined,
    })),
    combine: (results) => {
      const seen = new Set<string>();
      const jugadores: JugadorResponse[] = [];
      for (const r of results) {
        for (const j of r.data ?? []) {
          if (!seen.has(j.jugadorId)) {
            seen.add(j.jugadorId);
            jugadores.push(j);
          }
        }
      }
      return { jugadores, cargandoIntegrantes: results.some((r) => r.isLoading) };
    },
  });

  const tagPorEquipo = useMemo(() => {
    const m = new Map<string, string>();
    equipos?.forEach((e) => m.set(e.equipoId, e.tag));
    return m;
  }, [equipos]);

  const roles = useMemo(
    () => [...new Set(jugadores.map((j) => j.rol))].sort(),
    [jugadores]
  );

  const filtrados = useMemo(() => {
    const q = busqueda.trim().toLowerCase();
    return jugadores.filter((j) => {
      if (rolFiltro && j.rol !== rolFiltro) return false;
      if (!q) return true;
      return `${j.nickname} ${j.nombre} ${j.pais} ${j.rol}`.toLowerCase().includes(q);
    });
  }, [jugadores, busqueda, rolFiltro]);

  const cargando = loadingEquipos || cargandoIntegrantes;

  if (error) return <ErrorState error={error} onRetry={refetch} />;

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <div className="flex gap-2 max-w-sm">
          <Input
            placeholder="Buscar nickname, nombre, país o rol…"
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
          />
          <Button variant="outline" size="icon" disabled>
            <Search className="h-4 w-4" />
          </Button>
        </div>
        {roles.length > 0 && (
          <div className="flex flex-wrap gap-1.5">
            <button
              onClick={() => setRolFiltro("")}
              className={`eyebrow px-2 py-0.5 rounded hud-clip-sm border text-xs transition-colors ${rolFiltro === "" ? "border-violet/60 bg-violet/15 text-violet" : "border-line bg-elevated text-muted-foreground hover:text-foreground hover:border-violet/30"}`}
            >
              Todos los roles
            </button>
            {roles.map((r) => (
              <button
                key={r}
                onClick={() => setRolFiltro(r)}
                className={`eyebrow px-2 py-0.5 rounded hud-clip-sm border text-xs transition-colors ${rolFiltro === r ? "border-violet/60 bg-violet/15 text-violet" : "border-line bg-elevated text-muted-foreground hover:text-foreground hover:border-violet/30"}`}
              >
                {r}
              </button>
            ))}
          </div>
        )}
      </div>

      {cargando ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {[...Array(9)].map((_, i) => <Skeleton key={i} className="h-24" />)}
        </div>
      ) : filtrados.length === 0 ? (
        <EmptyState title="Sin jugadores" description="Ningún jugador coincide con el filtro." />
      ) : (
        <div>
          <p className="text-xs text-muted-foreground mb-3">{filtrados.length} jugadores</p>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {filtrados.map((j) => (
              <Link key={j.jugadorId} href={`/jugadores/${j.jugadorId}`}>
                <Card className="border-border hover:border-primary/30 transition-colors cursor-pointer">
                  <CardHeader className="py-3">
                    <div className="flex items-center gap-2">
                      <span className="hud-clip-sm border border-gold/30 bg-gold/10 text-gold font-mono text-[10px] px-1.5 py-0.5">{j.codigo}</span>
                      <span className="font-mono text-primary text-sm font-bold">{j.nickname}</span>
                      {j.equipoId && tagPorEquipo.get(j.equipoId) && (
                        <span className="hud-clip-sm border border-violet/30 bg-violet/10 text-violet font-mono text-[10px] px-1.5 py-0.5 ml-auto">
                          {tagPorEquipo.get(j.equipoId)}
                        </span>
                      )}
                    </div>
                    <p className="text-xs text-foreground mt-0.5">{j.nombre}</p>
                    <div className="flex items-center gap-2 mt-1">
                      <Badge variant="muted" className="w-fit">{j.rol}</Badge>
                      <span className="flex items-center gap-1 text-xs text-muted-foreground">
                        <Globe className="h-3 w-3" /> {j.pais}
                      </span>
                    </div>
                  </CardHeader>
                </Card>
              </Link>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function BuscarNickname() {
  const [input, setInput] = useState("");
  const [query, setQuery] = useState("");

  const { data, isLoading, error } = useQuery({
    queryKey: ["jugador", "nickname", query],
    queryFn: () => getJugadorPorNickname(query),
    enabled: !!query,
    retry: false,
  });

  const buscar = () => setQuery(input.trim());

  return (
    <div className="space-y-4">
      <div className="flex gap-2 max-w-sm">
        <Input
          placeholder="Nickname exacto…"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && buscar()}
        />
        <Button variant="outline" size="icon" onClick={buscar}>
          <Search className="h-4 w-4" />
        </Button>
      </div>
      {query && (
        isLoading ? <Skeleton className="h-24 w-full max-w-sm" /> :
        error ? (
          error instanceof ApiError && error.status === 404
            ? <p className="text-sm text-muted-foreground">No se encontró el jugador <span className="font-mono text-primary">{query}</span>.</p>
            : <ErrorState error={error} />
        ) : data ? (
          <Link href={`/jugadores/${data.jugadorId}`}>
            <Card className="max-w-sm border-primary/20 hover:border-primary/40 transition-colors cursor-pointer">
              <CardHeader className="pb-2">
                <div className="flex items-center gap-2">
                  <span className="hud-clip-sm border border-gold/30 bg-gold/10 text-gold font-mono text-[10px] px-1.5 py-0.5">{data.codigo}</span>
                  <span className="font-mono text-primary font-bold">{data.nickname}</span>
                </div>
              </CardHeader>
              <CardContent className="space-y-1">
                <p className="text-sm text-foreground">{data.nombre}</p>
                <div className="flex items-center gap-2">
                  <Globe className="h-3.5 w-3.5 text-muted-foreground" />
                  <span className="text-xs text-muted-foreground">{data.pais}</span>
                </div>
                <Badge variant="muted" className="mt-1">{data.rol}</Badge>
              </CardContent>
            </Card>
          </Link>
        ) : null
      )}
    </div>
  );
}

// El dato guarda país como código ISO-2 (KR, US, BR…), así que los chips
// consultan por código aunque muestren un nombre legible.
const PAISES_POPULARES = [
  { code: "KR", label: "Korea" },
  { code: "CN", label: "China" },
  { code: "BR", label: "Brazil" },
  { code: "US", label: "USA" },
  { code: "DK", label: "Denmark" },
  { code: "DE", label: "Germany" },
  { code: "FR", label: "France" },
  { code: "UA", label: "Ukraine" },
];

function BuscarPorPais() {
  const [input, setInput] = useState("");
  const [query, setQuery] = useState("KR");

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["jugadores", "pais", query],
    queryFn: () => getJugadoresPorPais(query),
    enabled: !!query,
  });

  const buscar = (pais?: string) => setQuery((pais ?? input).trim().toUpperCase());

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <div className="flex gap-2 max-w-sm">
          <Input
            placeholder="País ISO-2 (ej: KR, US, BR…)"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && buscar()}
          />
          <Button variant="outline" size="icon" onClick={() => buscar()}>
            <Search className="h-4 w-4" />
          </Button>
        </div>
        <div className="flex flex-wrap gap-1.5">
          {PAISES_POPULARES.map((p) => (
            <button
              key={p.code}
              onClick={() => { setInput(p.code); buscar(p.code); }}
              className={`eyebrow px-2 py-0.5 rounded hud-clip-sm border text-xs transition-colors ${query === p.code ? "border-violet/60 bg-violet/15 text-violet" : "border-line bg-elevated text-muted-foreground hover:text-foreground hover:border-violet/30"}`}
            >
              {p.label}
            </button>
          ))}
        </div>
      </div>
      {query && (
        isLoading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {[...Array(6)].map((_, i) => <Skeleton key={i} className="h-24" />)}
          </div>
        ) : error ? <ErrorState error={error} onRetry={refetch} /> :
        data?.length === 0 ? <EmptyState title="Sin jugadores" description={`No hay jugadores de ${query}.`} /> : (
          <div>
            <p className="text-xs text-muted-foreground mb-3">{data?.length} jugadores de {query}</p>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
              {data?.map((j) => (
                <Link key={j.jugadorId} href={`/jugadores/${j.jugadorId}`}>
                  <Card className="border-border hover:border-primary/30 transition-colors cursor-pointer">
                    <CardHeader className="py-3">
                      <div className="flex items-center gap-2">
                        <span className="hud-clip-sm border border-gold/30 bg-gold/10 text-gold font-mono text-[10px] px-1.5 py-0.5">{j.codigo}</span>
                        <span className="font-mono text-primary text-sm font-bold">{j.nickname}</span>
                      </div>
                      <p className="text-xs text-foreground mt-0.5">{j.nombre}</p>
                      <Badge variant="muted" className="w-fit mt-1">{j.rol}</Badge>
                    </CardHeader>
                  </Card>
                </Link>
              ))}
            </div>
          </div>
        )
      )}
    </div>
  );
}

export default function JugadoresPage() {
  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ roster</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <User className="w-7 h-7 text-violet" /> Jugadores
        </h1>
      </div>

      <Tabs defaultValue="todos">
        <TabsList>
          <TabsTrigger value="todos">Todos</TabsTrigger>
          <TabsTrigger value="nickname">Por nickname (Q1)</TabsTrigger>
          <TabsTrigger value="pais">Por país (Q2)</TabsTrigger>
        </TabsList>
        <TabsContent value="todos">
          <TodosJugadores />
        </TabsContent>
        <TabsContent value="nickname">
          <BuscarNickname />
        </TabsContent>
        <TabsContent value="pais">
          <BuscarPorPais />
        </TabsContent>
      </Tabs>
    </div>
  );
}
