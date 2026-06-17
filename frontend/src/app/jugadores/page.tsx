"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search, User, Globe } from "lucide-react";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { getJugadorPorNickname, getJugadoresPorPais } from "@/lib/api/equipos";
import { ApiError } from "@/lib/api/fetcher";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

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
          <Card className="max-w-sm border-primary/20">
            <CardHeader className="pb-2">
              <div className="flex items-center gap-2">
                <User className="h-5 w-5 text-primary" />
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
        ) : null
      )}
    </div>
  );
}

const PAISES_POPULARES = ["Korea", "USA", "Brazil", "China", "Germany", "Argentina", "Colombia", "France"];

function BuscarPorPais() {
  const [input, setInput] = useState("");
  const [query, setQuery] = useState("Korea");

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["jugadores", "pais", query],
    queryFn: () => getJugadoresPorPais(query),
    enabled: !!query,
  });

  const buscar = (pais?: string) => setQuery((pais ?? input).trim());

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <div className="flex gap-2 max-w-sm">
          <Input
            placeholder="País (ej: Bolivia, Korea…)"
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
              key={p}
              onClick={() => { setInput(p); buscar(p); }}
              className={`eyebrow px-2 py-0.5 rounded hud-clip-sm border text-xs transition-colors ${query === p ? "border-violet/60 bg-violet/15 text-violet" : "border-line bg-elevated text-muted-foreground hover:text-foreground hover:border-violet/30"}`}
            >
              {p}
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
                <Card key={j.jugadorId} className="border-border hover:border-primary/30 transition-colors">
                  <CardHeader className="py-3">
                    <div className="flex items-center gap-2">
                      <User className="h-4 w-4 text-muted-foreground" />
                      <span className="font-mono text-primary text-sm font-bold">{j.nickname}</span>
                    </div>
                    <p className="text-xs text-foreground mt-0.5">{j.nombre}</p>
                    <Badge variant="muted" className="w-fit mt-1">{j.rol}</Badge>
                  </CardHeader>
                </Card>
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

      <Tabs defaultValue="nickname">
        <TabsList>
          <TabsTrigger value="nickname">Por nickname (Q1)</TabsTrigger>
          <TabsTrigger value="pais">Por país (Q2)</TabsTrigger>
        </TabsList>
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
