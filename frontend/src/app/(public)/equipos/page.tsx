"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search, Users, Flag, ChevronRight } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { getEquiposPorFecha, getEquipoPorTag } from "@/lib/api/equipos";
import { formatDate } from "@/lib/utils";

export default function EquiposPage() {
  const [busquedaTag, setBusquedaTag] = useState("");
  const [tagBuscado, setTagBuscado] = useState("");

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

  if (isLoading) return (
    <div className="space-y-4">
      <Skeleton className="h-8 w-48" />
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
        {[...Array(6)].map((_, i) => <Skeleton key={i} className="h-20" />)}
      </div>
    </div>
  );

  if (error) return <ErrorState error={error} onRetry={refetch} />;

  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ roster</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <Users className="w-7 h-7 text-violet" /> Equipos
        </h1>
      </div>

      {/* Buscar por tag (Q5) */}
      <div className="flex gap-2 max-w-sm">
        <Input
          placeholder="Buscar por tag… (ej: T1)"
          value={busquedaTag}
          onChange={(e) => setBusquedaTag(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && setTagBuscado(busquedaTag.trim())}
        />
        <Button variant="outline" size="icon" onClick={() => setTagBuscado(busquedaTag.trim())}>
          <Search className="h-4 w-4" />
        </Button>
      </div>

      {tagBuscado && (
        buscandoTag ? <Skeleton className="h-16 max-w-sm" /> :
        equipoPorTag ? (
          <Link href={`/equipos/${equipoPorTag.equipoId}`}>
            <HudPanel className="max-w-sm p-4 hover:border-violet/50 transition-colors cursor-pointer">
              <div className="flex items-center gap-3">
                <span className="hud-clip-sm border border-violet/30 bg-violet/10 text-violet font-mono text-sm px-2 py-0.5">
                  {equipoPorTag.tag}
                </span>
                <div>
                  <p className="font-semibold text-foreground">{equipoPorTag.nombre}</p>
                  <p className="eyebrow mt-0.5">{equipoPorTag.pais}</p>
                </div>
              </div>
            </HudPanel>
          </Link>
        ) : (
          <p className="text-sm text-muted-foreground">No encontrado.</p>
        )
      )}

      {/* Lista por fecha (Q4) */}
      <HudPanel>
        <div className="px-4 py-3 border-b border-line flex items-center justify-between">
          <HudEyebrow>{equipos?.length ?? 0} equipos registrados</HudEyebrow>
        </div>
        {equipos?.length === 0 ? (
          <EmptyState title="Sin equipos" description="No hay equipos registrados." />
        ) : (
          <div className="divide-y divide-line">
            {equipos?.map((equipo) => (
              <Link
                key={equipo.equipoId}
                href={`/equipos/${equipo.equipoId}`}
                className="flex items-center justify-between px-4 py-3 hover:bg-secondary/40 transition-colors"
              >
                <div className="flex items-center gap-3">
                  <span className="hud-clip-sm border border-violet/30 bg-violet/10 text-violet font-mono text-xs px-2 py-0.5 shrink-0">
                    {equipo.tag}
                  </span>
                  <div>
                    <p className="text-sm font-semibold text-foreground">{equipo.nombre}</p>
                    <p className="eyebrow mt-0.5 flex items-center gap-1">
                      <Flag className="w-3 h-3" /> {equipo.pais}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  <span className="eyebrow">{formatDate(equipo.fechaCreacion)}</span>
                  <ChevronRight className="w-4 h-4 text-muted-foreground" />
                </div>
              </Link>
            ))}
          </div>
        )}
      </HudPanel>
    </div>
  );
}
