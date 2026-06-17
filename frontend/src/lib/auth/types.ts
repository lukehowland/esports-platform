export type Rol = "admin" | "organizador" | "capitan" | "fan";

export interface IdentidadAdmin {
  rol: "admin";
  username: string;
  nombre: string;
}

export interface IdentidadOrganizador {
  rol: "organizador";
  username: string;
  organizadorId: string;
  nombre: string;
}

export interface IdentidadCapitan {
  rol: "capitan";
  username: string;
  equipoId: string;
  nombre: string;
  // tag se resuelve al cargar el equipo, no viene del JWT
  tag?: string;
}

export interface IdentidadFan {
  rol: "fan";
  username: string;
  nombre: string;
}

export type Identidad =
  | IdentidadAdmin
  | IdentidadOrganizador
  | IdentidadCapitan
  | IdentidadFan;

export function isAdmin(i: Identidad | null): i is IdentidadAdmin {
  return i?.rol === "admin";
}

export function isOrganizador(i: Identidad | null): i is IdentidadOrganizador {
  return i?.rol === "organizador";
}

export function isCapitan(i: Identidad | null): i is IdentidadCapitan {
  return i?.rol === "capitan";
}

export function isFan(i: Identidad | null): i is IdentidadFan {
  return i?.rol === "fan";
}

// Puede gestionar (admin puede todo, org/cap tienen áreas propias)
export function puedeGestionar(i: Identidad | null): boolean {
  return i?.rol === "admin" || i?.rol === "organizador" || i?.rol === "capitan";
}
