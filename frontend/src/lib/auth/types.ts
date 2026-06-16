export type Rol = "organizador" | "capitan" | "fan";

export interface IdentidadOrganizador {
  rol: "organizador";
  organizadorId: string;
  nombre: string;
}

export interface IdentidadCapitan {
  rol: "capitan";
  equipoId: string;
  nombre: string;
  tag: string;
}

export interface IdentidadFan {
  rol: "fan";
}

export type Identidad = IdentidadOrganizador | IdentidadCapitan | IdentidadFan;

export function isOrganizador(i: Identidad | null): i is IdentidadOrganizador {
  return i?.rol === "organizador";
}

export function isCapitan(i: Identidad | null): i is IdentidadCapitan {
  return i?.rol === "capitan";
}

export function isFan(i: Identidad | null): i is IdentidadFan {
  return i?.rol === "fan";
}
