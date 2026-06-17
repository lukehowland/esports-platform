"use client";

import { useQuery } from "@tanstack/react-query";
import { Users, Flag } from "lucide-react";
import { RequireRole } from "@/lib/auth/require-role";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { StatTile } from "@/components/stat-tile";
import { EmptyState } from "@/components/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import { getEquiposPorFecha } from "@/lib/api/equipos";
import { formatDate } from "@/lib/utils";

export default function PanelEquiposPage() {
  return (
    <RequireRole roles={["admin"]}>
      <EquiposContent />
    </RequireRole>
  );
}

function EquiposContent() {
  const { data: equipos, isLoading } = useQuery({
    queryKey: ["equipos", "por-fecha"],
    queryFn: getEquiposPorFecha,
  });

  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ administración</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <Users className="w-7 h-7 text-violet" /> Equipos
        </h1>
      </div>

      <div className="grid grid-cols-3 gap-3">
        <StatTile value={equipos?.length ?? "—"} label="Total equipos" color="violet" />
      </div>

      <HudPanel>
        <div className="px-4 py-3 border-b border-line">
          <HudEyebrow>todos los equipos</HudEyebrow>
        </div>
        {isLoading ? (
          <div className="p-4 space-y-2">
            {[...Array(5)].map((_, i) => <Skeleton key={i} className="h-12" />)}
          </div>
        ) : equipos?.length === 0 ? (
          <EmptyState title="Sin equipos" description="No hay equipos registrados todavía." />
        ) : (
          <div className="divide-y divide-line">
            {equipos?.map((e) => (
              <div
                key={e.equipoId}
                className="flex items-center justify-between px-4 py-3"
              >
                <div className="flex items-center gap-3">
                  <span className="hud-clip-sm border border-violet/30 bg-violet/10 text-violet text-xs font-mono px-2 py-0.5">
                    {e.tag}
                  </span>
                  <div>
                    <p className="text-sm font-semibold text-foreground">{e.nombre}</p>
                    <p className="eyebrow mt-0.5 flex items-center gap-1">
                      <Flag className="w-3 h-3" /> {e.pais}
                    </p>
                  </div>
                </div>
                <span className="eyebrow">{formatDate(e.fechaCreacion)}</span>
              </div>
            ))}
          </div>
        )}
      </HudPanel>
    </div>
  );
}
