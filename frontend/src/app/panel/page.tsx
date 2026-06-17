"use client";

import { useQuery, useQueries } from "@tanstack/react-query";
import { Trophy, Users, Gamepad2, Zap, Swords } from "lucide-react";
import Link from "next/link";
import { useAuth } from "@/lib/auth/context";
import { isAdmin, isOrganizador } from "@/lib/auth/types";
import { StatTile } from "@/components/stat-tile";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { EmptyState } from "@/components/empty-state";
import {
  getOrganizadores, getTorneosPorFecha, getTorneosPorOrganizador, getPremiosPorTorneo,
} from "@/lib/api/torneos";
import { getEquiposPorFecha } from "@/lib/api/equipos";
import { getPartidasPorTorneo } from "@/lib/api/partidas";
import { Button } from "@/components/ui/button";
import { formatDate } from "@/lib/utils";

export default function PanelPage() {
  const { identidad } = useAuth();

  if (!identidad) return null;

  if (isAdmin(identidad)) return <AdminOverview />;
  if (isOrganizador(identidad)) return <OrgOverview organizadorId={identidad.organizadorId} />;
  return null;
}

// Mini-gráfico de barras horizontales sin dependencias externas.
function BarChart({ datos }: { datos: { label: string; value: number }[] }) {
  const max = Math.max(1, ...datos.map((d) => d.value));
  return (
    <div className="space-y-2">
      {datos.map((d) => (
        <div key={d.label} className="flex items-center gap-3">
          <span className="text-xs text-muted-foreground w-32 truncate shrink-0">{d.label}</span>
          <div className="flex-1 h-2 rounded bg-elevated overflow-hidden">
            <div className="h-full bg-violet rounded" style={{ width: `${(d.value / max) * 100}%` }} />
          </div>
          <span className="text-xs font-mono text-foreground w-6 text-right shrink-0">{d.value}</span>
        </div>
      ))}
    </div>
  );
}

function AdminOverview() {
  const { data: orgs } = useQuery({ queryKey: ["organizadores"], queryFn: getOrganizadores });
  const { data: equipos } = useQuery({ queryKey: ["equipos", "por-fecha"], queryFn: getEquiposPorFecha });
  const { data: torneos } = useQuery({ queryKey: ["torneos", "por-fecha"], queryFn: getTorneosPorFecha });

  // Total de partidas: fan-out por torneo (no hay endpoint "todas las partidas").
  const partidasQ = useQueries({
    queries: (torneos ?? []).map((t) => ({
      queryKey: ["partidas", "por-torneo", t.torneoId],
      queryFn: () => getPartidasPorTorneo(t.torneoId),
    })),
    combine: (results) => ({
      total: results.reduce((acc, r) => acc + (r.data?.length ?? 0), 0),
      listo: results.length === 0 || results.every((r) => r.isSuccess),
    }),
  });

  // Distribución de torneos por videojuego (top 6).
  const porJuego = Object.entries(
    (torneos ?? []).reduce<Record<string, number>>((acc, t) => {
      acc[t.nombreVideojuego] = (acc[t.nombreVideojuego] ?? 0) + 1;
      return acc;
    }, {})
  )
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => b.value - a.value)
    .slice(0, 6);

  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ panel de control</p>
        <h1 className="text-3xl font-display font-bold tracking-wide text-foreground">Administración</h1>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <StatTile value={equipos?.length ?? "—"} label="Equipos" color="violet" />
        <StatTile value={orgs?.length ?? "—"} label="Organizadores" color="lime" />
        <StatTile value={torneos?.length ?? "—"} label="Torneos" color="gold" />
        <StatTile value={torneos && partidasQ.listo ? partidasQ.total : "—"} label="Partidas" color="muted" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-3">
        <HudPanel className="p-4 space-y-4">
          <HudEyebrow>torneos por videojuego</HudEyebrow>
          {porJuego.length === 0 ? (
            <p className="text-sm text-muted-foreground">Sin torneos registrados.</p>
          ) : (
            <BarChart datos={porJuego} />
          )}
        </HudPanel>

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
      </div>

      <HudPanel>
        <div className="px-4 py-3 border-b border-line flex items-center justify-between">
          <HudEyebrow>torneos recientes</HudEyebrow>
          <Link href="/panel/torneos" className="eyebrow text-violet">ver todos →</Link>
        </div>
        {!torneos ? (
          <div className="p-4"><p className="text-sm text-muted-foreground">Cargando…</p></div>
        ) : torneos.length === 0 ? (
          <EmptyState title="Sin torneos" description="Todavía no hay torneos registrados." />
        ) : (
          <div className="divide-y divide-line">
            {torneos.slice(0, 5).map((t) => (
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

function OrgOverview({ organizadorId }: { organizadorId: string }) {
  const { data: torneos } = useQuery({
    queryKey: ["torneos", "por-organizador", organizadorId],
    queryFn: () => getTorneosPorOrganizador(organizadorId),
  });

  const partidasQ = useQueries({
    queries: (torneos ?? []).map((t) => ({
      queryKey: ["partidas", "por-torneo", t.torneoId],
      queryFn: () => getPartidasPorTorneo(t.torneoId),
    })),
    combine: (results) => ({
      total: results.reduce((acc, r) => acc + (r.data?.length ?? 0), 0),
      listo: results.length === 0 || results.every((r) => r.isSuccess),
    }),
  });

  const premiosQ = useQueries({
    queries: (torneos ?? []).map((t) => ({
      queryKey: ["premios", "por-torneo", t.torneoId],
      queryFn: () => getPremiosPorTorneo(t.torneoId),
    })),
    combine: (results) => ({
      total: results.reduce((acc, r) => acc + (r.data?.length ?? 0), 0),
      listo: results.length === 0 || results.every((r) => r.isSuccess),
    }),
  });

  return (
    <div className="space-y-6">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ panel de organizador</p>
        <h1 className="text-3xl font-display font-bold tracking-wide text-foreground">Mis Torneos</h1>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
        <StatTile value={torneos?.length ?? "—"} label="Torneos creados" color="violet" />
        <StatTile value={torneos && partidasQ.listo ? partidasQ.total : "—"} label="Partidas registradas" color="lime" />
        <StatTile value={torneos && premiosQ.listo ? premiosQ.total : "—"} label="Premios asignados" color="gold" />
      </div>

      <div className="flex gap-3">
        <Button asChild>
          <Link href="/panel/crear-torneo"><Trophy className="w-4 h-4 mr-1.5" /> Nuevo torneo</Link>
        </Button>
        <Button variant="outline" asChild>
          <Link href="/panel/videojuegos"><Gamepad2 className="w-4 h-4 mr-1.5" /> Gestionar videojuegos</Link>
        </Button>
      </div>

      <HudPanel className="divide-y divide-line">
        <div className="px-4 py-3 flex items-center gap-2">
          <Swords className="w-4 h-4 text-violet" />
          <HudEyebrow>mis torneos recientes</HudEyebrow>
        </div>
        {!torneos ? (
          <div className="px-4 py-3"><p className="text-sm text-muted-foreground">Cargando…</p></div>
        ) : torneos.length === 0 ? (
          <EmptyState title="Sin torneos" description="Creá tu primer torneo para empezar a gestionarlo." />
        ) : (
          torneos.slice(0, 5).map((t) => (
            <Link
              key={t.torneoId}
              href={`/panel/torneos/${t.torneoId}`}
              className="flex items-center justify-between px-4 py-3 hover:bg-secondary/40 transition-colors"
            >
              <div>
                <p className="text-sm font-semibold text-foreground">{t.nombreTorneo}</p>
                <p className="eyebrow mt-0.5">{t.nombreVideojuego}</p>
              </div>
              <span className="text-xs text-muted-foreground">→</span>
            </Link>
          ))
        )}
      </HudPanel>
    </div>
  );
}
