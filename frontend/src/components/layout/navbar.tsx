"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Gamepad2, LogOut, LayoutDashboard, Shield, Users, User, Trophy, Swords, BarChart3, BookOpen, Zap } from "lucide-react";
import { cn } from "@/lib/utils";
import { useAuth } from "@/lib/auth/context";
import { Button } from "@/components/ui/button";
import { isAdmin, isOrganizador, isCapitan } from "@/lib/auth/types";

const navLinks = [
  { href: "/equipos",       label: "Equipos",       icon: Users },
  { href: "/jugadores",     label: "Jugadores",      icon: User },
  { href: "/torneos",       label: "Torneos",        icon: Trophy },
  { href: "/videojuegos",   label: "Videojuegos",    icon: Gamepad2 },
  { href: "/organizadores", label: "Organizadores",  icon: Swords },
  { href: "/partidas",      label: "Partidas",       icon: Zap },
  { href: "/rankings",      label: "Rankings",       icon: BarChart3 },
  { href: "/manual",        label: "Manual",         icon: BookOpen },
];

function RolBadge({ identidad }: { identidad: NonNullable<ReturnType<typeof useAuth>["identidad"]> }) {
  if (isAdmin(identidad)) {
    return (
      <span className="hidden sm:flex items-center gap-1 rounded px-2 py-0.5 text-xs font-mono font-semibold bg-violet/15 text-violet-bright border border-violet/30">
        <Shield className="w-3 h-3" /> ADMIN
      </span>
    );
  }
  if (isOrganizador(identidad)) {
    return (
      <span className="hidden sm:flex items-center gap-1 rounded px-2 py-0.5 text-xs font-mono font-semibold bg-warning/10 text-warning border border-warning/30">
        <Trophy className="w-3 h-3" /> ORG
      </span>
    );
  }
  if (isCapitan(identidad)) {
    return (
      <span className="hidden sm:flex items-center gap-1 rounded px-2 py-0.5 text-xs font-mono font-semibold bg-lime/10 text-lime border border-lime/30">
        <Gamepad2 className="w-3 h-3" /> CAP
      </span>
    );
  }
  return (
    <span className="hidden sm:flex items-center gap-1 rounded px-2 py-0.5 text-xs font-mono text-muted-foreground border border-line">
      FAN
    </span>
  );
}

function panelLink(identidad: NonNullable<ReturnType<typeof useAuth>["identidad"]>) {
  if (isAdmin(identidad) || isOrganizador(identidad)) return "/panel";
  if (isCapitan(identidad)) return "/mi-equipo";
  return null;
}

export function Navbar() {
  const pathname = usePathname();
  const { identidad, logout } = useAuth();

  const dashLink = identidad ? panelLink(identidad) : null;

  return (
    <nav className="sticky top-0 z-40 border-b border-line bg-panel/90 backdrop-blur-sm">
      <div className="container mx-auto max-w-7xl px-4">
        <div className="flex h-14 items-center justify-between gap-4">

          {/* Logo */}
          <Link href="/" className="flex items-center gap-2 shrink-0 group">
            <Gamepad2 className="h-5 w-5 text-violet group-hover:text-violet-bright transition-colors" />
            <span className="text-sm font-display font-bold tracking-widest text-violet group-hover:text-violet-bright uppercase transition-colors">
              Esports
            </span>
          </Link>

          {/* Nav links */}
          <div className="flex items-center gap-0.5 overflow-x-auto flex-1 justify-center" style={{ scrollbarWidth: "none" }}>
            {navLinks.map(({ href, label }) => (
              <Link
                key={href}
                href={href}
                className={cn(
                  "shrink-0 rounded px-2.5 py-1.5 text-xs font-semibold transition-colors whitespace-nowrap tracking-wide",
                  pathname.startsWith(href)
                    ? "bg-violet/15 text-violet border border-violet/30"
                    : "text-muted-foreground hover:text-foreground hover:bg-secondary/60"
                )}
              >
                {label}
              </Link>
            ))}
          </div>

          {/* Auth zone */}
          <div className="flex items-center gap-2 shrink-0">
            {identidad ? (
              <>
                <RolBadge identidad={identidad} />
                {dashLink && (
                  <Button variant="outline" size="sm" asChild className="hidden sm:inline-flex">
                    <Link href={dashLink}>
                      <LayoutDashboard className="w-3.5 h-3.5 mr-1" /> Ir a mi área
                    </Link>
                  </Button>
                )}
                <Button variant="ghost" size="icon" onClick={logout} title="Cerrar sesión" className="hud-clip-sm">
                  <LogOut className="h-4 w-4" />
                </Button>
              </>
            ) : (
              <Button size="sm" asChild>
                <Link href="/login">Ingresar</Link>
              </Button>
            )}
          </div>

        </div>
      </div>
    </nav>
  );
}
