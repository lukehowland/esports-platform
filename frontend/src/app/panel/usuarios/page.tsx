"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useQuery, useMutation } from "@tanstack/react-query";
import { UserCog, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { RequireRole } from "@/lib/auth/require-role";
import { registrarUsuario, type RegistrarUsuarioDto } from "@/lib/api/auth";
import { getOrganizadores } from "@/lib/api/torneos";
import { getEquiposPorFecha } from "@/lib/api/equipos";
import { ApiError } from "@/lib/api/fetcher";
import { toast } from "sonner";

const ROLES = ["admin", "organizador", "capitan", "fan"] as const;

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
  const [rolSeleccionado, setRolSeleccionado] = useState<string>("fan");
  const [serverError, setServerError] = useState<string | null>(null);

  const { data: organizadores } = useQuery({
    queryKey: ["organizadores"],
    queryFn: getOrganizadores,
    enabled: rolSeleccionado === "organizador",
  });

  const { data: equipos } = useQuery({
    queryKey: ["equipos", "por-fecha"],
    queryFn: getEquiposPorFecha,
    enabled: rolSeleccionado === "capitan",
  });

  const {
    register, handleSubmit, watch, setValue, reset,
    formState: { errors, isSubmitting }
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { rol: "fan" },
  });

  const rolActual = watch("rol");

  const mutation = useMutation({
    mutationFn: (dto: RegistrarUsuarioDto) => registrarUsuario(dto),
    onSuccess: () => {
      toast.success("Usuario registrado correctamente");
      reset();
      setRolSeleccionado("fan");
      setServerError(null);
    },
    onError: (err) => {
      const msg = err instanceof ApiError ? err.detail : "Error al registrar usuario";
      setServerError(msg);
    },
  });

  const onSubmit = (data: FormData) => {
    setServerError(null);
    const dto: RegistrarUsuarioDto = {
      username:      data.username,
      password:      data.password,
      rol:           data.rol,
      nombreDisplay: data.nombreDisplay,
      organizadorId: data.rol === "organizador" ? data.organizadorId : undefined,
      equipoId:      data.rol === "capitan" ? data.equipoId : undefined,
    };
    mutation.mutate(dto);
  };

  return (
    <div className="space-y-6 max-w-lg">
      <div>
        <p className="eyebrow text-violet mb-1">▰▰ administración</p>
        <h1 className="text-3xl font-display font-bold tracking-wide flex items-center gap-3">
          <UserCog className="w-7 h-7 text-violet" /> Registrar Usuario
        </h1>
      </div>

      <HudPanel className="p-5">
        <HudEyebrow className="block mb-4">nuevo usuario de plataforma</HudEyebrow>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

          <div className="grid grid-cols-2 gap-4">
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
                setRolSeleccionado(v);
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

          <Button type="submit" disabled={isSubmitting} className="w-full">
            {isSubmitting && <Loader2 className="w-4 h-4 animate-spin" />}
            Registrar usuario
          </Button>
        </form>
      </HudPanel>
    </div>
  );
}
