"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Users, UserPlus, Trophy, Loader2, Flag, Ticket } from "lucide-react";
import { useAuth } from "@/lib/auth/context";
import { isCapitan } from "@/lib/auth/types";
import { getEquipoPorId, getIntegrantesPorEquipo, agregarJugador, type AgregarJugadorDto } from "@/lib/api/equipos";
import { getTorneosPorEquipo, getTorneosPorFecha, inscribirEquipo } from "@/lib/api/torneos";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { StatTile } from "@/components/stat-tile";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ApiError } from "@/lib/api/fetcher";
import { toast } from "sonner";

const jugadorSchema = z.object({
  nickname: z.string().min(1, "Requerido").max(32),
  nombre:   z.string().min(1, "Requerido"),
  pais:     z.string().length(2, "Código ISO-2"),
  rol:      z.string().min(1, "Requerido"),
});
type JugadorForm = z.infer<typeof jugadorSchema>;

export default function MiEquipoPage() {
  const { identidad } = useAuth();
  const qc = useQueryClient();
  const capitan = isCapitan(identidad) ? identidad : null;
  const equipoId = capitan?.equipoId ?? "";

  const { data: equipo } = useQuery({
    queryKey: ["equipo", equipoId],
    queryFn: () => getEquipoPorId(equipoId),
    enabled: !!equipoId,
  });

  const { data: integrantes } = useQuery({
    queryKey: ["integrantes", equipoId],
    queryFn: () => getIntegrantesPorEquipo(equipoId),
    enabled: !!equipoId,
  });

  const { data: torneos } = useQuery({
    queryKey: ["torneos", "por-equipo", equipoId],
    queryFn: () => getTorneosPorEquipo(equipoId),
    enabled: !!equipoId,
  });

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<JugadorForm>({
    resolver: zodResolver(jugadorSchema),
  });

  const [addError, setAddError] = useState<string | null>(null);

  const addMutation = useMutation({
    mutationFn: (dto: AgregarJugadorDto) => agregarJugador(equipoId, dto),
    onSuccess: () => {
      toast.success("Jugador agregado");
      reset();
      setAddError(null);
      qc.invalidateQueries({ queryKey: ["integrantes", equipoId] });
    },
    onError: (err) => {
      const msg = err instanceof ApiError ? err.detail : "Error al agregar jugador";
      setAddError(msg);
    },
  });

  // Inscribir mi equipo en un torneo. El equipo es siempre el del capitán (no editable):
  // solo se elige el torneo. El backend valida el ownership del equipo en el token.
  const { data: torneosDisponibles } = useQuery({
    queryKey: ["torneos", "por-fecha"],
    queryFn: getTorneosPorFecha,
  });
  const [torneoSel, setTorneoSel] = useState("");

  const inscribirMutation = useMutation({
    mutationFn: () => inscribirEquipo(torneoSel, equipoId),
    onSuccess: () => {
      toast.success("Equipo inscrito. Tus rankings se actualizarán en breve.");
      setTorneoSel("");
      qc.invalidateQueries({ queryKey: ["torneos", "por-equipo", equipoId] });
    },
    onError: (err) => {
      const msg = err instanceof ApiError ? err.detail : "Error al inscribir el equipo";
      toast.error(msg);
    },
  });

  if (!equipo) {
    return (
      <div className="flex items-center justify-center min-h-[40vh]">
        <div className="w-8 h-8 border-2 border-violet rounded-full border-t-transparent animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-3xl">
      {/* Header */}
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ cockpit de capitán</p>
        <h1 className="text-3xl font-display font-bold tracking-wide text-foreground flex items-center gap-3">
          <span className="hud-clip-sm border border-lime/40 bg-lime/10 text-lime px-3 py-1 text-xl">
            {equipo.tag}
          </span>
          {equipo.nombre}
        </h1>
        <div className="flex items-center gap-2 mt-2">
          <Flag className="w-3.5 h-3.5 text-muted-foreground" />
          <span className="eyebrow">{equipo.pais}</span>
        </div>
      </div>

      {/* Stats rápidas */}
      <div className="grid grid-cols-3 gap-3">
        <StatTile value={integrantes?.length ?? "—"} label="Jugadores" color="violet" />
        <StatTile value={torneos?.length ?? "—"} label="Torneos" color="lime" />
        <StatTile value="—" label="Victorias" color="gold" />
      </div>

      {/* Tabs de gestión */}
      <Tabs defaultValue="roster">
        <TabsList>
          <TabsTrigger value="roster">
            <Users className="w-3.5 h-3.5 mr-1.5" /> Roster
          </TabsTrigger>
          <TabsTrigger value="agregar">
            <UserPlus className="w-3.5 h-3.5 mr-1.5" /> Agregar jugador
          </TabsTrigger>
          <TabsTrigger value="inscribir">
            <Ticket className="w-3.5 h-3.5 mr-1.5" /> Inscribir en torneo
          </TabsTrigger>
          <TabsTrigger value="torneos">
            <Trophy className="w-3.5 h-3.5 mr-1.5" /> Torneos
          </TabsTrigger>
        </TabsList>

        {/* Roster */}
        <TabsContent value="roster" className="mt-4">
          <HudPanel>
            {integrantes && integrantes.length > 0 ? (
              <div className="divide-y divide-line">
                {integrantes.map((j) => (
                  <a
                    key={j.jugadorId}
                    href={`/jugadores/${j.jugadorId}`}
                    className="flex items-center justify-between px-4 py-3 hover:bg-secondary/40 transition-colors"
                  >
                    <div className="flex items-center gap-3 min-w-0">
                      <span className="hud-clip-sm border border-gold/30 bg-gold/10 text-gold text-[10px] font-mono px-1.5 py-0.5 shrink-0">{j.codigo}</span>
                      <div className="min-w-0">
                        <p className="text-sm font-semibold text-foreground truncate">{j.nickname}</p>
                        <p className="eyebrow mt-0.5">{j.nombre} · {j.pais}</p>
                      </div>
                    </div>
                    <span className="hud-clip-sm border border-violet/30 bg-violet/10 text-violet text-xs font-mono px-2 py-0.5 shrink-0">
                      {j.rol}
                    </span>
                  </a>
                ))}
              </div>
            ) : (
              <div className="px-4 py-8 text-center text-muted-foreground text-sm">
                Sin jugadores aún. Agregá el primer integrante.
              </div>
            )}
          </HudPanel>
        </TabsContent>

        {/* Agregar jugador */}
        <TabsContent value="agregar" className="mt-4">
          <HudPanel className="p-5">
            <HudEyebrow className="block mb-4">nuevo integrante</HudEyebrow>
            <form
              onSubmit={handleSubmit((d) => addMutation.mutate(d))}
              className="space-y-4"
            >
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label className="eyebrow">Nickname</Label>
                  <Input placeholder="s1mple" {...register("nickname")} />
                  {errors.nickname && <p className="text-xs text-destructive">{errors.nickname.message}</p>}
                </div>
                <div className="space-y-1.5">
                  <Label className="eyebrow">Nombre completo</Label>
                  <Input placeholder="Oleksandr Kostyliev" {...register("nombre")} />
                  {errors.nombre && <p className="text-xs text-destructive">{errors.nombre.message}</p>}
                </div>
                <div className="space-y-1.5">
                  <Label className="eyebrow">País (ISO-2)</Label>
                  <Input placeholder="UA" maxLength={2} {...register("pais")} />
                  {errors.pais && <p className="text-xs text-destructive">{errors.pais.message}</p>}
                </div>
                <div className="space-y-1.5">
                  <Label className="eyebrow">Rol en equipo</Label>
                  <Input placeholder="AWP / IGL / FLEX…" {...register("rol")} />
                  {errors.rol && <p className="text-xs text-destructive">{errors.rol.message}</p>}
                </div>
              </div>
              {addError && (
                <div className="rounded border border-destructive/40 bg-destructive/10 px-4 py-3">
                  <p className="text-sm text-destructive">{addError}</p>
                </div>
              )}
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting && <Loader2 className="w-4 h-4 animate-spin" />}
                <UserPlus className="w-4 h-4" /> Agregar jugador
              </Button>
            </form>
          </HudPanel>
        </TabsContent>

        {/* Inscribir en torneo */}
        <TabsContent value="inscribir" className="mt-4">
          <HudPanel className="p-5 space-y-4">
            <HudEyebrow className="block">inscribir a {equipo.nombre}</HudEyebrow>
            <p className="text-sm text-muted-foreground">
              Elegí el torneo en el que querés inscribir tu equipo. La inscripción es siempre
              para <span className="text-foreground font-semibold">[{equipo.tag}] {equipo.nombre}</span>.
            </p>
            <div className="space-y-1.5 max-w-md">
              <Label className="eyebrow">Torneo</Label>
              <Select value={torneoSel} onValueChange={setTorneoSel}>
                <SelectTrigger><SelectValue placeholder="Seleccionar torneo…" /></SelectTrigger>
                <SelectContent>
                  {torneosDisponibles?.map((t) => (
                    <SelectItem key={t.torneoId} value={t.torneoId}>{t.nombreTorneo}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <Button
              onClick={() => inscribirMutation.mutate()}
              disabled={!torneoSel || inscribirMutation.isPending}
            >
              {inscribirMutation.isPending && <Loader2 className="w-4 h-4 animate-spin" />}
              <Ticket className="w-4 h-4" /> Inscribir mi equipo
            </Button>
          </HudPanel>
        </TabsContent>

        {/* Torneos */}
        <TabsContent value="torneos" className="mt-4">
          <HudPanel>
            {torneos && torneos.length > 0 ? (
              <div className="divide-y divide-line">
                {torneos.map((t) => (
                  <a
                    key={t.torneoId}
                    href={`/torneos/${t.torneoId}`}
                    className="flex items-center justify-between px-4 py-3 hover:bg-secondary/40 transition-colors"
                  >
                    <div>
                      <p className="text-sm font-semibold text-foreground">{t.nombreTorneo}</p>
                      <p className="eyebrow mt-0.5">{t.nombreVideojuego}</p>
                    </div>
                    <span className="text-xs text-muted-foreground">→</span>
                  </a>
                ))}
              </div>
            ) : (
              <div className="px-4 py-8 text-center text-muted-foreground text-sm">
                Tu equipo no está inscrito en torneos todavía.
              </div>
            )}
          </HudPanel>
        </TabsContent>
      </Tabs>
    </div>
  );
}
