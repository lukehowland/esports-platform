"use client";

import { useParams } from "next/navigation";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { Users, Flag, ArrowLeft } from "lucide-react";
import { RequireRole } from "@/lib/auth/require-role";
import { HudPanel } from "@/components/hud-panel";
import { Skeleton } from "@/components/ui/skeleton";
import { ErrorState } from "@/components/error-state";
import { RosterManager } from "@/components/roster-manager";
import { getEquipoPorId } from "@/lib/api/equipos";

export default function PanelEquipoDetallePage() {
  return (
    <RequireRole roles={["admin"]}>
      <EquipoDetalleContent />
    </RequireRole>
  );
}

function EquipoDetalleContent() {
  const { id } = useParams<{ id: string }>();

  const { data: equipo, isLoading, error } = useQuery({
    queryKey: ["equipo", id],
    queryFn: () => getEquipoPorId(id),
  });

  if (isLoading) return <Skeleton className="h-64 w-full max-w-3xl" />;
  if (error || !equipo) return <ErrorState error={error} />;

  return (
    <div className="space-y-6 max-w-3xl">
      <div>
        <Link href="/panel/equipos" className="eyebrow text-violet flex items-center gap-1">
          <ArrowLeft className="w-3.5 h-3.5" /> equipos
        </Link>
        <h1 className="mt-2 text-3xl font-display font-bold tracking-wide text-foreground flex items-center gap-3">
          <span className="hud-clip-sm border border-violet/40 bg-violet/10 text-violet px-3 py-1 text-xl font-mono">
            {equipo.tag}
          </span>
          {equipo.nombre}
        </h1>
        <p className="eyebrow mt-2 flex items-center gap-1"><Flag className="w-3 h-3" /> {equipo.pais}</p>
      </div>

      <div className="flex items-center gap-2">
        <Users className="w-4 h-4 text-violet" />
        <p className="eyebrow">gestión de roster (RF-03)</p>
      </div>

      <RosterManager equipoId={equipo.equipoId} equipoNombre={equipo.nombre} esAdmin />
    </div>
  );
}
