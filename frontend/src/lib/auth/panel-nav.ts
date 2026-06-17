import type { Rol } from "./types";

export interface PanelNavItem {
  href: string;
  label: string;
  icon: string;   // nombre del ícono de lucide-react (string para serializar)
}

const adminNav: PanelNavItem[] = [
  { href: "/panel",                  label: "Overview",      icon: "LayoutDashboard" },
  { href: "/panel/equipos",          label: "Equipos",       icon: "Users" },
  { href: "/panel/organizadores",    label: "Organizadores", icon: "Building2" },
  { href: "/panel/videojuegos",      label: "Videojuegos",   icon: "Gamepad2" },
  { href: "/panel/torneos",          label: "Torneos",       icon: "Trophy" },
  { href: "/panel/usuarios",         label: "Usuarios",      icon: "UserCog" },
];

const organizadorNav: PanelNavItem[] = [
  { href: "/panel",                  label: "Mi Panel",      icon: "LayoutDashboard" },
  { href: "/panel/mis-torneos",      label: "Mis torneos",   icon: "Trophy" },
  { href: "/panel/crear-torneo",     label: "Nuevo torneo",  icon: "PlusCircle" },
];

export function getPanelNav(rol: Rol): PanelNavItem[] {
  if (rol === "admin")       return adminNav;
  if (rol === "organizador") return organizadorNav;
  return [];
}
