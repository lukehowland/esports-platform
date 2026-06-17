"use client";

import { useQuery } from "@tanstack/react-query";
import { Trophy, Users, Gamepad2, Zap } from "lucide-react";
import Link from "next/link";
import { useAuth } from "@/lib/auth/context";
import { isAdmin, isOrganizador } from "@/lib/auth/types";
import { StatTile } from "@/components/stat-tile";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { getOrganizadores } from "@/lib/api/torneos";
import { getEquiposPorFecha } from "@/lib/api/equipos";
import { getTorneosPorOrganizador } from "@/lib/api/torneos";
import { Button } from "@/components/ui/button";

export default function PanelPage() {
  const { identidad } = useAuth();

  if (!identidad) return null;

  if (isAdmin(identidad)) return <AdminOverview />;
  if (isOrganizador(identidad)) return <OrgOverview organizadorId={identidad.organizadorId} />;
  return null;
}

function AdminOverview() {
  const { data: orgs } = useQuery({ queryKey: ["organizadores"], queryFn: getOrganizadores });
  const { data: equipos } = useQuery({ queryKey: ["equipos", "por-fecha"], queryFn: getEquiposPorFecha });

  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ panel de control</p>
        <h1 className="text-3xl font-display font-bold tracking-wide text-foreground">Administración</h1>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <StatTile value={equipos?.length ?? "—"} label="Equipos" color="violet" />
        <StatTile value={orgs?.length ?? "—"} label="Organizadores" color="lime" />
        <StatTile value="—" label="Torneos" color="gold" />
        <StatTile value="—" label="Partidas" color="muted" />
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <HudPanel className="p-4 space-y-3">
          <HudEyebrow>acciones rápidas</HudEyebrow>
          <div className="grid grid-cols-2 gap-2 pt-1">
            <Button size="sm" variant="outline" asChild>
              <Link href="/panel/equipos"><Users className="w-3.5 h-3.5 mr-1" /> Equipos</Link>
            </Button>
            <Button size="sm" variant="outline" asChild>
              <Link href="/panel/organizadores"><Trophy className="w-3.5 h-3.5 mr-1" /> Organizadores</Link>
            </Button>
            <Button size="sm" variant="outline" asChild>
              <Link href="/panel/videojuegos"><Gamepad2 className="w-3.5 h-3.5 mr-1" /> Videojuegos</Link>
            </Button>
            <Button size="sm" variant="lime" asChild>
              <Link href="/panel/usuarios"><Zap className="w-3.5 h-3.5 mr-1" /> Usuarios</Link>
            </Button>
          </div>
        </HudPanel>

        <HudPanel className="p-4 space-y-3">
          <HudEyebrow>explorar plataforma</HudEyebrow>
          <div className="grid grid-cols-2 gap-2 pt-1">
            <Button size="sm" variant="ghost" asChild>
              <Link href="/torneos">Torneos</Link>
            </Button>
            <Button size="sm" variant="ghost" asChild>
              <Link href="/rankings">Rankings</Link>
            </Button>
            <Button size="sm" variant="ghost" asChild>
              <Link href="/partidas">Partidas</Link>
            </Button>
            <Button size="sm" variant="ghost" asChild>
              <Link href="/manual">Manual</Link>
            </Button>
          </div>
        </HudPanel>
      </div>
    </div>
  );
}

function OrgOverview({ organizadorId }: { organizadorId: string }) {
  const { data: torneos } = useQuery({
    queryKey: ["torneos", "por-organizador", organizadorId],
    queryFn: () => getTorneosPorOrganizador(organizadorId),
  });

  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ panel de organizador</p>
        <h1 className="text-3xl font-display font-bold tracking-wide text-foreground">Mis Torneos</h1>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
        <StatTile value={torneos?.length ?? "—"} label="Torneos creados" color="violet" />
        <StatTile value="—" label="Partidas registradas" color="lime" />
        <StatTile value="—" label="Premios asignados" color="gold" />
      </div>

      <div className="flex gap-3">
        <Button asChild>
          <Link href="/panel/crear-torneo"><Trophy className="w-4 h-4 mr-1.5" /> Nuevo torneo</Link>
        </Button>
        <Button variant="outline" asChild>
          <Link href="/panel/videojuegos"><Gamepad2 className="w-4 h-4 mr-1.5" /> Gestionar videojuegos</Link>
        </Button>
      </div>

      {torneos && torneos.length > 0 && (
        <HudPanel className="divide-y divide-line">
          <div className="px-4 py-3">
            <HudEyebrow>mis torneos recientes</HudEyebrow>
          </div>
          {torneos.slice(0, 5).map((t) => (
            <Link
              key={t.torneoId}
              href={`/torneos/${t.torneoId}`}
              className="flex items-center justify-between px-4 py-3 hover:bg-secondary/40 transition-colors"
            >
              <div>
                <p className="text-sm font-semibold text-foreground">{t.nombre}</p>
                <p className="eyebrow mt-0.5">{t.codigo}</p>
              </div>
              <span className="text-xs text-muted-foreground">→</span>
            </Link>
          ))}
        </HudPanel>
      )}
    </div>
  );
}
