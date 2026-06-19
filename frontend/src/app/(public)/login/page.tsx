"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Gamepad2, Loader2, Zap } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/lib/auth/context";
import { ApiError } from "@/lib/api/fetcher";
import { toast } from "sonner";

const schema = z.object({
  username: z.string().min(1, "Ingresá tu usuario"),
  password: z.string().min(1, "Ingresá tu contraseña"),
});

type FormData = z.infer<typeof schema>;

// Accesos rápidos para demo/sustentación
const QUICK_LOGINS = [
  { label: "Admin",       username: "admin",    password: "admin-dev-password",  rol: "admin",       color: "border-violet/50 hover:border-violet text-violet" },
  { label: "Organizador", username: "org_riot", password: "OrgDemo2024",          rol: "organizador", color: "border-warning/50 hover:border-warning text-warning" },
  { label: "Capitán",     username: "cap_t1",   password: "CapDemo2024",           rol: "capitan",     color: "border-lime/50 hover:border-lime text-lime" },
  { label: "Fan",         username: "fan_demo", password: "FanDemo2024",           rol: "fan",         color: "border-line/50 hover:border-muted-foreground text-muted-foreground" },
] as const;

export default function LoginPage() {
  const { login } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);
  const { register, handleSubmit, setValue, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const doLogin = async (data: FormData) => {
    setServerError(null);
    try {
      await login(data.username, data.password);
      toast.success("¡Bienvenido!");
    } catch (err) {
      const msg = err instanceof ApiError ? err.detail : "Error al iniciar sesión";
      setServerError(msg);
    }
  };

  return (
    <div className="min-h-[calc(100vh-4rem)] flex items-center justify-center px-4">
      <div className="w-full max-w-sm space-y-8">

        {/* Header */}
        <div className="text-center space-y-3">
          <div className="inline-flex items-center justify-center w-14 h-14 rounded-xl bg-violet/10 border border-violet/30 mb-2">
            <Gamepad2 className="w-7 h-7 text-violet" />
          </div>
          <h1 className="text-3xl font-display font-bold tracking-wide text-foreground">
            INGRESAR
          </h1>
          <p className="text-sm text-muted-foreground">
            Usá tus credenciales de la plataforma eSports
          </p>
        </div>

        {/* Formulario real */}
        <form onSubmit={handleSubmit(doLogin)} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="username" className="text-xs font-mono uppercase tracking-widest text-muted-foreground">
              Usuario
            </Label>
            <Input
              id="username"
              type="text"
              autoComplete="username"
              placeholder="tu_usuario"
              {...register("username")}
              aria-invalid={!!errors.username}
            />
            {errors.username && (
              <p className="text-xs text-destructive">{errors.username.message}</p>
            )}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="password" className="text-xs font-mono uppercase tracking-widest text-muted-foreground">
              Contraseña
            </Label>
            <Input
              id="password"
              type="password"
              autoComplete="current-password"
              placeholder="••••••••"
              {...register("password")}
              aria-invalid={!!errors.password}
            />
            {errors.password && (
              <p className="text-xs text-destructive">{errors.password.message}</p>
            )}
          </div>

          {serverError && (
            <div className="rounded-lg border border-destructive/40 bg-destructive/10 px-4 py-3">
              <p className="text-sm text-destructive">{serverError}</p>
            </div>
          )}

          <Button type="submit" className="w-full" disabled={isSubmitting}>
            {isSubmitting && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
            Iniciar sesión
          </Button>
        </form>

        {/* Accesos rápidos demo */}
        <div className="space-y-3">
          <div className="flex items-center gap-3">
            <div className="flex-1 h-px bg-line" />
            <span className="flex items-center gap-1.5 text-xs font-mono text-muted-foreground uppercase tracking-wider">
              <Zap className="w-3 h-3" /> Acceso rápido demo
            </span>
            <div className="flex-1 h-px bg-line" />
          </div>
          <div className="grid grid-cols-2 gap-2">
            {QUICK_LOGINS.map(({ label, username, password, color }) => (
              <button
                key={label}
                type="button"
                disabled={isSubmitting}
                onClick={() => {
                  setValue("username", username);
                  setValue("password", password);
                  setServerError(null);
                }}
                className={`rounded-lg border px-3 py-2 text-xs font-mono font-semibold uppercase tracking-wider transition-colors ${color} disabled:opacity-50 disabled:cursor-not-allowed`}
              >
                {label}
              </button>
            ))}
          </div>
          <p className="text-center text-xs text-muted-foreground/60">
            Credenciales de demo — solo para presentación académica
          </p>
        </div>

      </div>
    </div>
  );
}
