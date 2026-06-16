import { fetcher } from "./fetcher";

export interface PartidaPorTorneoResponse {
  partidaId: string;
  nombreLocal: string;
  nombreVisitante: string;
  resultado: string;
  fecha: string;
}

export interface PartidaPorEquipoResponse {
  partidaId: string;
  nombreTorneo: string;
  rival: string;
  resultado: string;
  fecha: string;
}

export interface PartidaPorFechaResponse {
  partidaId: string;
  torneoId: string;
  nombreLocal: string;
  nombreVisitante: string;
  resultado: string;
}

export interface PartidaPorRivalesResponse {
  partidaId: string;
  equipoLocalId: string;
  resultado: string;
  fecha: string;
}

export interface RegistrarPartidaDto {
  torneoId: string;
  nombreTorneo: string;
  equipoLocalId: string;
  nombreLocal: string;
  equipoVisitanteId: string;
  nombreVisitante: string;
  equipoGanadorId: string;
  resultado: string;
  fecha: string;
}

// Q16
export const getPartidasPorTorneo = (torneoId: string) =>
  fetcher<PartidaPorTorneoResponse[]>(`/api/partidas/por-torneo/${torneoId}`);

// Q17
export const getPartidasPorEquipo = (equipoId: string) =>
  fetcher<PartidaPorEquipoResponse[]>(`/api/partidas/por-equipo/${equipoId}`);

// Q18
export const getPartidasPorFecha = (dia: string) =>
  fetcher<PartidaPorFechaResponse[]>(`/api/partidas/por-fecha/${dia}`);

// Q19
export const getPartidasEntre = (equipoId: string, rivalId: string) =>
  fetcher<PartidaPorRivalesResponse[]>(`/api/partidas/entre/${equipoId}/${rivalId}`);

// Mutación
export const registrarPartida = (data: RegistrarPartidaDto) =>
  fetcher<unknown>("/api/partidas", { method: "POST", body: JSON.stringify(data) });
