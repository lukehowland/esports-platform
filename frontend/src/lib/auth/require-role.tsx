"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "./context";
import type { Rol } from "./types";

interface Props {
  roles: Rol[];
  children: React.ReactNode;
  redirectTo?: string;
}

/**
 * Protege contenido que requiere autenticación y un rol específico.
 * Redirige a /login si no hay sesión, o a / si el rol no es suficiente.
 */
export function RequireRole({ roles, children, redirectTo = "/login" }: Props) {
  const { identidad, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (loading) return;
    if (!identidad) { router.replace(redirectTo); return; }
    if (!roles.includes(identidad.rol as Rol)) { router.replace("/"); }
  }, [identidad, loading, roles, redirectTo, router]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <div className="w-8 h-8 border-2 border-violet rounded-full border-t-transparent animate-spin" />
      </div>
    );
  }

  if (!identidad || !roles.includes(identidad.rol as Rol)) return null;

  return <>{children}</>;
}
