"use client";

import Link from "next/link";
import { Gamepad2, LogOut, ExternalLink } from "lucide-react";
import { useAuth } from "@/lib/auth/context";
import { Button } from "@/components/ui/button";

/**
 * Barra superior mínima de los workspaces privados (panel, cockpit).
 * Deliberadamente NO incluye los links de browse del sitio público: dentro
 * de un workspace solo se navega con la navegación propia del rol.
 */
export function WorkspaceTopbar({ section }: { section: string }) {
  const { identidad, logout } = useAuth();
  const nombre = identidad && "nombre" in identidad ? identidad.nombre : "";

  return (
    <header className="sticky top-0 z-40 border-b border-line bg-panel/90 backdrop-blur-sm">
      <div className="container mx-auto max-w-7xl px-4">
        <div className="flex h-14 items-center justify-between gap-4">

          {/* Marca + sección */}
          <Link href="/" className="flex items-center gap-2.5 shrink-0 group" title="Ir al sitio público">
            <Gamepad2 className="h-5 w-5 text-violet group-hover:text-violet-bright transition-colors" />
            <span className="text-sm font-display font-bold tracking-widest text-violet group-hover:text-violet-bright uppercase transition-colors">
              Esports
            </span>
            <span className="hidden sm:inline text-line">·</span>
            <span className="hidden sm:inline eyebrow text-muted-foreground">{section}</span>
          </Link>

          {/* Identidad + acciones */}
          <div className="flex items-center gap-2 shrink-0">
            <Link
              href="/"
              className="hidden sm:flex items-center gap-1 rounded px-2 py-1.5 text-xs font-semibold text-muted-foreground hover:text-foreground transition-colors"
              title="Explorar el sitio público"
            >
              <ExternalLink className="w-3.5 h-3.5" /> Sitio público
            </Link>
            <span className="hidden sm:block text-sm font-semibold text-foreground truncate max-w-[14rem]">
              {nombre}
            </span>
            <Button variant="ghost" size="icon" onClick={logout} title="Cerrar sesión" className="hud-clip-sm">
              <LogOut className="h-4 w-4" />
            </Button>
          </div>

        </div>
      </div>
    </header>
  );
}
