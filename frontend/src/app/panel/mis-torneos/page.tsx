"use client";

import { useQuery } from "@tanstack/react-query";
import { Trophy } from "lucide-react";
import Link from "next/link";
import { useAuth } from "@/lib/auth/context";
import { isOrganizador } from "@/lib/auth/types";
import { RequireRole } from "@/lib/auth/require-role";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { StatTile } from "@/components/stat-tile";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { getTorneosPorOrganizador } from "@/lib/api/torneos";
import { formatDate } from "@/lib/utils";

export default function MisTorneosPage() {
  return (
    <RequireRole roles={["organizador"]}>
      <MisTorneosContent />
    </RequireRole>
  );
}

function MisTorneosContent() {
  const { identidad } = useAuth();
  const org = isOrganizador(identidad) ? identidad : null;
  const organizadorId = org?.organizadorId ?? "";

  const { data: torneos, isLoading } = useQuery({
    queryKey: ["torneos", "por-organizador", organizadorId],
    queryFn: () => getTorneosPorOrganizador(organizadorId),
    enabled: !!organizadorId,
  });

  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ mis torneos</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <Trophy className="w-7 h-7 text-violet" /> Mis Torneos
        </h1>
      </div>

      <div className="flex items-center justify-between">
        <div className="grid grid-cols-2 gap-3">
          <StatTile value={torneos?.length ?? "—"} label="Torneos creados" color="violet" />
        </div>
        <Button asChild>
          <Link href="/panel/crear-torneo">
            <Trophy className="w-4 h-4 mr-1.5" /> Nuevo torneo
          </Link>
        </Button>
      </div>

      <HudPanel>
        <div className="px-4 py-3 border-b border-line">
          <HudEyebrow>historial de torneos</HudEyebrow>
        </div>
        {isLoading ? (
          <div className="p-4 space-y-2">
            {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-12" />)}
          </div>
        ) : torneos?.length === 0 ? (
          <EmptyState title="Sin torneos" description="Todavía no creaste ningún torneo." />
        ) : (
          <div className="divide-y divide-line">
            {torneos?.map((t) => (
              <Link
                key={t.torneoId}
                href={`/panel/torneos/${t.torneoId}`}
                className="flex items-center justify-between px-4 py-3 hover:bg-secondary/40 transition-colors"
              >
                <div>
                  <p className="text-sm font-semibold text-foreground">{t.nombreTorneo}</p>
                  <p className="eyebrow mt-0.5">{t.nombreVideojuego}</p>
                </div>
                <span className="eyebrow">{formatDate(t.fechaInicio)}</span>
              </Link>
            ))}
          </div>
        )}
      </HudPanel>
    </div>
  );
}
