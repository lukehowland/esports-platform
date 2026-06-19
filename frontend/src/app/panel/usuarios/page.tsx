"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { UserCog, Loader2, Plus, Trash2, ShieldAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter,
} from "@/components/ui/dialog";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { StatTile } from "@/components/stat-tile";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/empty-state";
import { RequireRole } from "@/lib/auth/require-role";
import { useAuth } from "@/lib/auth/context";
import {
  registrarUsuario, listarUsuarios, eliminarUsuario,
  type RegistrarUsuarioDto, type UsuarioResumenResponse,
} from "@/lib/api/auth";
import { getOrganizadores } from "@/lib/api/torneos";
import { getEquiposPorFecha } from "@/lib/api/equipos";
import { ApiError } from "@/lib/api/fetcher";
import { toast } from "sonner";

const ROLES = ["admin", "organizador", "capitan", "fan"] as const;

const ROL_COLOR: Record<string, string> = {
  admin:       "border-violet/40 bg-violet/10 text-violet",
  organizador: "border-lime/40 bg-lime/10 text-lime",
  capitan:     "border-gold/40 bg-gold/10 text-gold",
  fan:         "border-line bg-elevated text-muted-foreground",
};

const schema = z.object({
  username:      z.string().min(3, "Mínimo 3 caracteres").max(64),
  password:      z.string().min(8, "Mínimo 8 caracteres"),
  rol:           z.enum(ROLES),
  nombreDisplay: z.string().min(1, "Requerido"),
  organizadorId: z.string().optional(),
  equipoId:      z.string().optional(),
}).refine((d) => {
  if (d.rol === "organizador") return !!d.organizadorId;
  return true;
}, { message: "Seleccioná un organizador", path: ["organizadorId"] })
.refine((d) => {
  if (d.rol === "capitan") return !!d.equipoId;
  return true;
}, { message: "Seleccioná un equipo", path: ["equipoId"] });

type FormData = z.infer<typeof schema>;

export default function UsuariosPage() {
  return (
    <RequireRole roles={["admin"]}>
      <UsuariosContent />
    </RequireRole>
  );
}

function UsuariosContent() {
  const qc = useQueryClient();
  const { identidad } = useAuth();
  const yo = identidad && "username" in identidad ? identidad.username : "";

  const [modalAbierto, setModalAbierto] = useState(false);
  const [aEliminar, setAEliminar] = useState<UsuarioResumenResponse | null>(null);

  const { data: usuarios, isLoading } = useQuery({
    queryKey: ["usuarios"],
    queryFn: listarUsuarios,
  });

  const eliminar = useMutation({
    mutationFn: (username: string) => eliminarUsuario(username),
    onSuccess: () => {
      toast.success("Usuario eliminado");
      setAEliminar(null);
      qc.invalidateQueries({ queryKey: ["usuarios"] });
    },
    onError: (err) => {
      toast.error(err instanceof ApiError ? err.detail : "No se pudo eliminar el usuario");
    },
  });

  const totalPorRol = (rol: string) => (usuarios ?? []).filter((u) => u.rol === rol).length;

  return (
    <div className="space-y-6">
      <div className="flex items-end justify-between gap-4">
        <div>
          <p className="eyebrow text-violet mb-1">▰▰ administración</p>
          <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
            <UserCog className="w-7 h-7 text-violet" /> Usuarios
          </h1>
        </div>
        <Button onClick={() => setModalAbierto(true)}>
          <Plus className="w-4 h-4" /> Registrar usuario
        </Button>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <StatTile value={usuarios?.length ?? "—"} label="Total usuarios" color="violet" />
        <StatTile value={usuarios ? totalPorRol("organizador") : "—"} label="Organizadores" color="lime" />
        <StatTile value={usuarios ? totalPorRol("capitan") : "—"} label="Capitanes" color="gold" />
        <StatTile value={usuarios ? totalPorRol("fan") : "—"} label="Fans" color="muted" />
      </div>

      <HudPanel>
        <div className="px-4 py-3 border-b border-line">
          <HudEyebrow>todos los usuarios de la plataforma</HudEyebrow>
        </div>
        {isLoading ? (
          <div className="p-4 space-y-2">
            {[...Array(5)].map((_, i) => <Skeleton key={i} className="h-12" />)}
          </div>
        ) : usuarios?.length === 0 ? (
          <EmptyState title="Sin usuarios" description="Registrá el primer usuario arriba." />
        ) : (
          <div className="divide-y divide-line">
            {usuarios?.map((u) => (
              <div key={u.username} className="flex items-center justify-between px-4 py-3 gap-3">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <p className="text-sm font-semibold text-foreground truncate">{u.nombre}</p>
                    {u.username === yo && (
                      <span className="eyebrow text-violet text-[10px]">vos</span>
                    )}
                  </div>
                  <p className="eyebrow mt-0.5 font-mono">@{u.username}</p>
                </div>
                <div className="flex items-center gap-3 shrink-0">
                  <span className={`hud-clip-sm border text-xs font-mono px-2 py-0.5 ${ROL_COLOR[u.rol] ?? ROL_COLOR.fan}`}>
                    {u.rol}
                  </span>
                  <Button
                    size="icon"
                    variant="ghost"
                    aria-label={`Eliminar ${u.username}`}
                    disabled={u.username === yo}
                    onClick={() => setAEliminar(u)}
                  >
                    <Trash2 className="w-4 h-4 text-destructive" />
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}
      </HudPanel>

      <RegistrarUsuarioModal
        abierto={modalAbierto}
        onClose={() => setModalAbierto(false)}
        onCreado={() => {
          setModalAbierto(false);
          qc.invalidateQueries({ queryKey: ["usuarios"] });
        }}
      />

      <Dialog open={!!aEliminar} onOpenChange={(o) => !o && setAEliminar(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <ShieldAlert className="w-5 h-5 text-destructive" /> Eliminar usuario
            </DialogTitle>
            <DialogDescription>
              Vas a eliminar a <span className="font-mono text-foreground">@{aEliminar?.username}</span>.
              Esta acción no se puede deshacer.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setAEliminar(null)}>Cancelar</Button>
            <Button
              variant="destructive"
              disabled={eliminar.isPending}
              onClick={() => aEliminar && eliminar.mutate(aEliminar.username)}
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

function RegistrarUsuarioModal({
  abierto, onClose, onCreado,
}: { abierto: boolean; onClose: () => void; onCreado: () => void }) {
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register, handleSubmit, watch, setValue, reset,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { rol: "fan" },
  });

  const rolActual = watch("rol");

  const { data: organizadores } = useQuery({
    queryKey: ["organizadores"],
    queryFn: getOrganizadores,
    enabled: rolActual === "organizador",
  });

  const { data: equipos } = useQuery({
    queryKey: ["equipos", "por-fecha"],
    queryFn: getEquiposPorFecha,
    enabled: rolActual === "capitan",
  });

  const mutation = useMutation({
    mutationFn: (dto: RegistrarUsuarioDto) => registrarUsuario(dto),
    onSuccess: () => {
      toast.success("Usuario registrado correctamente");
      reset({ rol: "fan" });
      setServerError(null);
      onCreado();
    },
    onError: (err) => {
      setServerError(err instanceof ApiError ? err.detail : "Error al registrar usuario");
    },
  });

  const onSubmit = (data: FormData) => {
    setServerError(null);
    mutation.mutate({
      username:      data.username,
      password:      data.password,
      rol:           data.rol,
      nombreDisplay: data.nombreDisplay,
      organizadorId: data.rol === "organizador" ? data.organizadorId : undefined,
      equipoId:      data.rol === "capitan" ? data.equipoId : undefined,
    });
  };

  const handleClose = () => {
    reset({ rol: "fan" });
    setServerError(null);
    onClose();
  };

  return (
    <Dialog open={abierto} onOpenChange={(o) => !o && handleClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Registrar usuario</DialogTitle>
          <DialogDescription>Nuevo usuario de la plataforma con su rol.</DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label className="eyebrow">Usuario</Label>
              <Input placeholder="usuario_demo" {...register("username")} />
              {errors.username && <p className="text-xs text-destructive">{errors.username.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label className="eyebrow">Contraseña</Label>
              <Input type="password" placeholder="••••••••" {...register("password")} />
              {errors.password && <p className="text-xs text-destructive">{errors.password.message}</p>}
            </div>
          </div>

          <div className="space-y-1.5">
            <Label className="eyebrow">Nombre para mostrar</Label>
            <Input placeholder="Nombre Apellido" {...register("nombreDisplay")} />
            {errors.nombreDisplay && <p className="text-xs text-destructive">{errors.nombreDisplay.message}</p>}
          </div>

          <div className="space-y-1.5">
            <Label className="eyebrow">Rol</Label>
            <Select
              value={rolActual}
              onValueChange={(v) => {
                setValue("rol", v as FormData["rol"]);
                setValue("organizadorId", undefined);
                setValue("equipoId", undefined);
              }}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {ROLES.map((r) => (
                  <SelectItem key={r} value={r}>{r}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {rolActual === "organizador" && (
            <div className="space-y-1.5">
              <Label className="eyebrow">Organizador vinculado</Label>
              <Select onValueChange={(v) => setValue("organizadorId", v)}>
                <SelectTrigger>
                  <SelectValue placeholder="Seleccioná un organizador…" />
                </SelectTrigger>
                <SelectContent>
                  {(organizadores ?? []).map((o) => (
                    <SelectItem key={o.organizadorId} value={o.organizadorId}>{o.nombre}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.organizadorId && <p className="text-xs text-destructive">{errors.organizadorId.message}</p>}
            </div>
          )}

          {rolActual === "capitan" && (
            <div className="space-y-1.5">
              <Label className="eyebrow">Equipo vinculado</Label>
              <Select onValueChange={(v) => setValue("equipoId", v)}>
                <SelectTrigger>
                  <SelectValue placeholder="Seleccioná un equipo…" />
                </SelectTrigger>
                <SelectContent>
                  {(equipos ?? []).map((e) => (
                    <SelectItem key={e.equipoId} value={e.equipoId}>[{e.tag}] {e.nombre}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.equipoId && <p className="text-xs text-destructive">{errors.equipoId.message}</p>}
            </div>
          )}

          {serverError && (
            <div className="rounded border border-destructive/40 bg-destructive/10 px-4 py-3">
              <p className="text-sm text-destructive">{serverError}</p>
            </div>
          )}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose}>Cancelar</Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="w-4 h-4 animate-spin" />}
              Registrar
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
