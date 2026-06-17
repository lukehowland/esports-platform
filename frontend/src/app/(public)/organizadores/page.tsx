"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Building2, ChevronDown, ChevronRight } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { getOrganizadores, getTorneosPorOrganizador } from "@/lib/api/torneos";
import { formatDate } from "@/lib/utils";

function TorneosOrganizador({ organizadorId, nombre }: { organizadorId: string; nombre: string }) {
  const [exp, setExp] = useState(false);
  const { data, isLoading } = useQuery({
    queryKey: ["torneos", "por-org", organizadorId],
    queryFn: () => getTorneosPorOrganizador(organizadorId),
    enabled: exp,
  });
  return (
    <div>
      <button onClick={() => setExp(!exp)} className="flex items-center gap-1.5 text-xs text-primary hover:underline mt-2">
        {exp ? <ChevronDown className="h-3 w-3" /> : <ChevronRight className="h-3 w-3" />}
        Ver torneos de {nombre}
      </button>
      {exp && (
        <div className="mt-2 pl-2 border-l border-border space-y-1">
          {isLoading ? <Skeleton className="h-12" /> :
            data?.length === 0 ? <p className="text-xs text-muted-foreground">Sin torneos.</p> :
            data?.map((t) => (
              <div key={t.torneoId} className="flex items-center justify-between py-1">
                <span className="text-xs text-foreground">{t.nombreTorneo}</span>
                <div className="flex items-center gap-2">
                  <span className="text-xs text-muted-foreground">{t.nombreVideojuego}</span>
                  <span className="text-xs text-muted-foreground">{formatDate(t.fechaInicio)}</span>
                </div>
              </div>
            ))
          }
        </div>
      )}
    </div>
  );
}

export default function OrganizadoresPage() {
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["organizadores"],
    queryFn: getOrganizadores,
  });

  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ organizadores</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <Building2 className="w-7 h-7 text-violet" /> Organizadores
        </h1>
      </div>

      <HudPanel>
        <div className="px-4 py-3 border-b border-line">
          <HudEyebrow>{data?.length ?? "…"} organizadores</HudEyebrow>
        </div>
        {isLoading ? (
          <div className="p-4 space-y-2">{[...Array(3)].map((_, i) => <Skeleton key={i} className="h-16" />)}</div>
        ) : error ? <ErrorState error={error} onRetry={refetch} /> :
        data?.length === 0 ? <EmptyState title="Sin organizadores" description="No hay organizadores registrados." /> : (
          <div className="divide-y divide-line">
            {data?.map((org) => (
              <div key={org.organizadorId} className="px-4 py-3">
                <div className="flex items-center gap-2 mb-1">
                  <Building2 className="h-4 w-4 text-violet" />
                  <p className="font-semibold text-foreground">{org.nombre}</p>
                </div>
                <TorneosOrganizador organizadorId={org.organizadorId} nombre={org.nombre} />
              </div>
            ))}
          </div>
        )}
      </HudPanel>
    </div>
  );
}
