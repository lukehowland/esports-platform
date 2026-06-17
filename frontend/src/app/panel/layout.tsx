"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard, Users, Building2, Gamepad2, Trophy, UserCog, PlusCircle,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useAuth } from "@/lib/auth/context";
import { RequireRole } from "@/lib/auth/require-role";
import { getPanelNav } from "@/lib/auth/panel-nav";
import type { Rol } from "@/lib/auth/types";

const ICON_MAP: Record<string, React.ElementType> = {
  LayoutDashboard, Users, Building2, Gamepad2, Trophy, UserCog, PlusCircle,
};

export default function PanelLayout({ children }: { children: React.ReactNode }) {
  const { identidad } = useAuth();
  const pathname = usePathname();

  const rol = (identidad?.rol ?? "fan") as Rol;
  const navItems = getPanelNav(rol);

  const rolLabel = rol === "admin" ? "Administrador" : "Organizador";
  const nombre = identidad && "nombre" in identidad ? identidad.nombre : "";

  return (
    <RequireRole roles={["admin", "organizador"]}>
      <div className="flex gap-6 min-h-[calc(100vh-4rem-3rem)]">

        {/* Sidebar */}
        <aside className="w-52 shrink-0 space-y-1 pt-1">
          {/* Identidad */}
          <div className="hud-clip border border-violet/30 bg-violet/5 px-4 py-3 mb-4">
            <p className="eyebrow text-violet mb-0.5">{rolLabel}</p>
            <p className="text-sm font-semibold text-foreground truncate">{nombre}</p>
          </div>

          {/* Links */}
          {navItems.map(({ href, label, icon }) => {
            const Icon = ICON_MAP[icon] ?? LayoutDashboard;
            const isActive = pathname === href || (href !== "/panel" && pathname.startsWith(href));
            return (
              <Link
                key={href}
                href={href}
                className={cn(
                  "flex items-center gap-2.5 px-3 py-2 rounded text-sm font-semibold transition-colors",
                  isActive
                    ? "bg-violet/15 text-violet border border-violet/30"
                    : "text-muted-foreground hover:text-foreground hover:bg-secondary/60"
                )}
              >
                <Icon className="w-4 h-4 shrink-0" />
                {label}
              </Link>
            );
          })}
        </aside>

        {/* Contenido */}
        <div className="flex-1 min-w-0 pt-1">
          {children}
        </div>

      </div>
    </RequireRole>
  );
}
