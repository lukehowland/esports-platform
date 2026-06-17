"use client";

import { useState, useEffect } from "react";
import { useQueries, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Gamepad2, Loader2, Plus, Pencil, Trash2, ShieldAlert } from "lucide-react";
import { toast } from "sonner";
import { RequireRole } from "@/lib/auth/require-role";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { StatTile } from "@/components/stat-tile";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { ErrorState } from "@/components/error-state";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter,
} from "@/components/ui/dialog";
import {
  getVideojuegosPorGenero, crearVideojuego, editarVideojuego, eliminarVideojuego,
} from "@/lib/api/torneos";
import { ApiError } from "@/lib/api/fetcher";

const GENEROS = ["MOBA", "FPS", "BATTLE_ROYALE", "RTS", "FIGHTING", "SPORTS", "RPG"];
const TODOS = "TODOS";

interface Juego { videojuegoId: string; nombre: string; genero: string; }

const schema = z.object({
  nombre: z.string().min(1, "Requerido").max(120),
  genero: z.string().min(1, "Requerido"),
});
type Form = z.infer<typeof schema>;

export default function PanelVideojuegosPage() {
  return (
    <RequireRole roles={["admin"]}>
      <VideojuegosContent />
    </RequireRole>
  );
}

function VideojuegosContent() {
  const qc = useQueryClient();
  const [filtro, setFiltro] = useState(TODOS);
  const [modalCrear, setModalCrear] = useState(false);
  const [aEditar, setAEditar] = useState<Juego | null>(null);
  const [aEliminar, setAEliminar] = useState<Juego | null>(null);

  // Query-first: no existe "listar todos". Fan-out por los 7 géneros (Q8), se
  // etiqueta cada juego con su género y se filtra en memoria. "TODOS" muestra el
  // catálogo completo y los chips filtran sin volver a pedir.
  const { juegos, cargando, error } = useQueries({
    queries: GENEROS.map((g) => ({
      queryKey: ["videojuegos", g],
      queryFn: () => getVideojuegosPorGenero(g),
    })),
    combine: (results) => {
      const seen = new Set<string>();
      const juegos: Juego[] = [];
      results.forEach((r, i) => {
        for (const vg of r.data ?? []) {
          if (!seen.has(vg.videojuegoId)) {
            seen.add(vg.videojuegoId);
            juegos.push({ videojuegoId: vg.videojuegoId, nombre: vg.nombre, genero: GENEROS[i] });
          }
        }
      });
      return {
        juegos,
        cargando: results.some((r) => r.isLoading),
        error: results.find((r) => r.error)?.error ?? null,
      };
    },
  });

  const refrescar = () => qc.invalidateQueries({ queryKey: ["videojuegos"] });

  const eliminar = useMutation({
    mutationFn: (j: Juego) => eliminarVideojuego(j.videojuegoId),
    onSuccess: () => {
      toast.success("Videojuego eliminado");
      setAEliminar(null);
      refrescar();
    },
    onError: (err) => {
      toast.error(err instanceof ApiError ? err.detail : "No se pudo eliminar el videojuego");
    },
  });

  const visibles = filtro === TODOS ? juegos : juegos.filter((j) => j.genero === filtro);

  return (
    <div className="space-y-6 max-w-2xl">
      <div className="flex items-end justify-between gap-4">
        <div>
          <p className="eyebrow text-violet mb-1">▰▰ catálogo</p>
          <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
            <Gamepad2 className="w-7 h-7 text-violet" /> Videojuegos
          </h1>
        </div>
        <Button onClick={() => setModalCrear(true)}>
          <Plus className="w-4 h-4" /> Nuevo videojuego
        </Button>
      </div>

      <div className="grid grid-cols-3 gap-3">
        <StatTile value={cargando ? "—" : juegos.length} label="Total videojuegos" color="violet" />
      </div>

      <HudPanel>
        <div className="px-4 py-3 border-b border-line">
          <HudEyebrow>catálogo completo · filtrar por género</HudEyebrow>
        </div>
        <div className="px-4 py-3 flex flex-wrap gap-1.5 border-b border-line">
          {[TODOS, ...GENEROS].map((g) => (
            <button
              key={g}
              onClick={() => setFiltro(g)}
              className={`eyebrow px-2 py-0.5 rounded hud-clip-sm border text-xs transition-colors ${filtro === g ? "border-violet/60 bg-violet/15 text-violet" : "border-line bg-elevated text-muted-foreground hover:text-foreground hover:border-violet/30"}`}
            >
              {g}
            </button>
          ))}
        </div>
        {cargando ? (
          <div className="p-4 space-y-2">
            {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-10" />)}
          </div>
        ) : error ? (
          <ErrorState error={error} />
        ) : visibles.length === 0 ? (
          <EmptyState title="Sin videojuegos" description={filtro === TODOS ? "Creá el primer videojuego arriba." : `No hay videojuegos de género ${filtro}.`} />
        ) : (
          <div className="divide-y divide-line">
            {visibles.map((v) => (
              <div key={v.videojuegoId} className="flex items-center justify-between px-4 py-3 gap-3">
                <div className="flex items-center gap-3 min-w-0">
                  <p className="text-sm font-semibold text-foreground truncate">{v.nombre}</p>
                  <span className="hud-clip-sm border border-lime/30 bg-lime/10 text-lime text-xs font-mono px-2 py-0.5 shrink-0">
                    {v.genero}
                  </span>
                </div>
                <div className="flex items-center gap-1 shrink-0">
                  <Button size="icon" variant="ghost" aria-label={`Editar ${v.nombre}`} onClick={() => setAEditar(v)}>
                    <Pencil className="w-4 h-4 text-violet" />
                  </Button>
                  <Button size="icon" variant="ghost" aria-label={`Eliminar ${v.nombre}`} onClick={() => setAEliminar(v)}>
                    <Trash2 className="w-4 h-4 text-destructive" />
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}
      </HudPanel>

      <VideojuegoModal
        abierto={modalCrear}
        juego={null}
        onClose={() => setModalCrear(false)}
        onGuardado={() => { setModalCrear(false); refrescar(); }}
      />
      <VideojuegoModal
        abierto={!!aEditar}
        juego={aEditar}
        onClose={() => setAEditar(null)}
        onGuardado={() => { setAEditar(null); refrescar(); }}
      />

      <Dialog open={!!aEliminar} onOpenChange={(o) => !o && setAEliminar(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <ShieldAlert className="w-5 h-5 text-destructive" /> Eliminar videojuego
            </DialogTitle>
            <DialogDescription>
              Vas a eliminar <span className="font-semibold text-foreground">{aEliminar?.nombre}</span>.
              No se puede eliminar si tiene torneos asociados.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setAEliminar(null)}>Cancelar</Button>
            <Button
              variant="destructive"
              disabled={eliminar.isPending}
              onClick={() => aEliminar && eliminar.mutate(aEliminar)}
            >
              {eliminar.isPending && <Loader2 className="w-4 h-4 animate-spin" />}
              Eliminar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function VideojuegoModal({
  abierto, juego, onClose, onGuardado,
}: { abierto: boolean; juego: Juego | null; onClose: () => void; onGuardado: () => void }) {
  // Latcheamos el juego para que el contenido (título/valores) no parpadee a
  // "Nuevo" mientras el diálogo de edición reproduce su animación de cierre.
  const [display, setDisplay] = useState<Juego | null>(juego);
  useEffect(() => { if (juego) setDisplay(juego); }, [juego]);
  const esEdicion = !!display;
  const [serverError, setServerError] = useState<string | null>(null);

  const { register, handleSubmit, setValue, watch, reset, formState: { errors } } = useForm<Form>({
    resolver: zodResolver(schema),
    values: display ? { nombre: display.nombre, genero: display.genero } : { nombre: "", genero: "MOBA" },
  });

  const generoActual = watch("genero");

  const guardar = useMutation({
    mutationFn: (d: Form) =>
      esEdicion
        ? editarVideojuego(display!.videojuegoId, { nombre: d.nombre, genero: d.genero })
        : crearVideojuego({ nombre: d.nombre, genero: d.genero }),
    onSuccess: () => {
      toast.success(esEdicion ? "Videojuego actualizado" : "Videojuego creado");
      setServerError(null);
      reset({ nombre: "", genero: "MOBA" });
      onGuardado();
    },
    onError: (err) => {
      setServerError(err instanceof ApiError ? err.detail : "No se pudo guardar el videojuego");
    },
  });

  const handleClose = () => { reset({ nombre: "", genero: "MOBA" }); setServerError(null); onClose(); };

  return (
    <Dialog open={abierto} onOpenChange={(o) => !o && handleClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>{esEdicion ? "Editar videojuego" : "Nuevo videojuego"}</DialogTitle>
          <DialogDescription>
            {esEdicion
              ? "No se puede modificar si ya tiene torneos asociados."
              : "Registrá un videojuego en el catálogo."}
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit((d) => guardar.mutate(d))} className="space-y-4">
          <div className="space-y-1.5">
            <Label className="eyebrow">Nombre</Label>
            <Input placeholder="Counter-Strike 2" {...register("nombre")} />
            {errors.nombre && <p className="text-xs text-destructive">{errors.nombre.message}</p>}
          </div>
          <div className="space-y-1.5">
            <Label className="eyebrow">Género</Label>
            <Select value={generoActual} onValueChange={(v) => setValue("genero", v, { shouldValidate: true })}>
              <SelectTrigger>
                <SelectValue placeholder="Seleccioná un género…" />
              </SelectTrigger>
              <SelectContent>
                {GENEROS.map((g) => (
                  <SelectItem key={g} value={g}>{g}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            {errors.genero && <p className="text-xs text-destructive">{errors.genero.message}</p>}
          </div>
          {serverError && (
            <div className="rounded border border-destructive/40 bg-destructive/10 px-4 py-3">
              <p className="text-sm text-destructive">{serverError}</p>
            </div>
          )}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose}>Cancelar</Button>
            <Button type="submit" disabled={guardar.isPending}>
              {guardar.isPending && <Loader2 className="w-4 h-4 animate-spin" />}
              {esEdicion ? "Guardar" : "Crear"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
