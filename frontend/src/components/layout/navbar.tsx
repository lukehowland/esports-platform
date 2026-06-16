"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { Gamepad2, LogOut, User, Trophy, Users, Swords, BarChart3, BookOpen } from "lucide-react";
import { cn } from "@/lib/utils";
import { useAuth } from "@/lib/auth/context";
import { Button } from "@/components/ui/button";
import { isOrganizador, isCapitan } from "@/lib/auth/types";

const navLinks = [
  { href: "/equipos", label: "Equipos", icon: Users },
  { href: "/jugadores", label: "Jugadores", icon: User },
  { href: "/torneos", label: "Torneos", icon: Trophy },
  { href: "/videojuegos", label: "Videojuegos", icon: Gamepad2 },
  { href: "/organizadores", label: "Organizadores", icon: Swords },
  { href: "/partidas", label: "Partidas", icon: Swords },
  { href: "/rankings", label: "Rankings", icon: BarChart3 },
  { href: "/manual", label: "Manual", icon: BookOpen },
];

export function Navbar() {
  const pathname = usePathname();
  const router = useRouter();
  const { identidad, logout } = useAuth();

  const handleLogout = () => {
    logout();
    router.push("/login");
  };

  const rolLabel = identidad
    ? isOrganizador(identidad)
      ? `🎯 ${identidad.nombre}`
      : isCapitan(identidad)
      ? `🛡️ ${identidad.nombre}`
      : "👁️ Fan"
    : null;

  return (
    <nav className="sticky top-0 z-40 border-b border-border bg-card/80 backdrop-blur-sm">
      <div className="container mx-auto max-w-7xl px-4">
        <div className="flex h-14 items-center justify-between gap-4">
          {/* Logo */}
          <Link href="/" className="flex items-center gap-2 shrink-0">
            <Gamepad2 className="h-6 w-6 text-primary" />
            <span className="text-sm font-bold tracking-widest text-primary uppercase">
              Esports
            </span>
          </Link>

          {/* Nav links — scrollable en móvil */}
          <div className="flex items-center gap-0.5 overflow-x-auto scrollbar-hide flex-1 justify-center">
            {navLinks.map(({ href, label }) => (
              <Link
                key={href}
                href={href}
                className={cn(
                  "shrink-0 rounded-md px-2.5 py-1.5 text-xs font-medium transition-colors whitespace-nowrap",
                  pathname.startsWith(href)
                    ? "bg-secondary text-primary"
                    : "text-muted-foreground hover:text-foreground hover:bg-secondary/50"
                )}
              >
                {label}
              </Link>
            ))}
          </div>

          {/* Rol / auth */}
          <div className="flex items-center gap-2 shrink-0">
            {identidad ? (
              <>
                <span className="hidden sm:block text-xs text-muted-foreground max-w-[120px] truncate">
                  {rolLabel}
                </span>
                <Button variant="ghost" size="icon" onClick={handleLogout} title="Cerrar sesión">
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
