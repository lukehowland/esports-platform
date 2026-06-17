import { fetcher } from "./fetcher";

// ─── Tipos ───────────────────────────────────────────────────────────────────

export interface LoginResponse {
  token: string;
  rol: string;
  organizadorId?: string;
  equipoId?: string;
  nombre: string;
  username: string;
  expiraEn: string;
}

export interface MeResponse {
  username: string;
  rol: string;
  nombre: string;
  organizadorId?: string;
  equipoId?: string;
}

export interface RegistrarUsuarioDto {
  username: string;
  password: string;
  rol: string;
  nombreDisplay: string;
  organizadorId?: string;
  equipoId?: string;
}

// ─── Endpoints ───────────────────────────────────────────────────────────────

export function login(username: string, password: string): Promise<LoginResponse> {
  return fetcher<LoginResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ username, password }),
  });
}

export function me(): Promise<MeResponse> {
  return fetcher<MeResponse>("/api/auth/me");
}

export function registrarUsuario(dto: RegistrarUsuarioDto): Promise<void> {
  return fetcher<void>("/api/auth/register", {
    method: "POST",
    body: JSON.stringify(dto),
  });
}
