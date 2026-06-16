import { fetcher } from "./fetcher";

export interface RankingEquipoResponse {
  equipoId: string;
  totalTorneos: number;
  nombreEquipo?: string;
}

export interface RankingVictoriasResponse {
  equipoId: string;
  totalVictorias: number;
  nombreEquipo?: string;
}

export interface RankingJugadorResponse {
  jugadorId: string;
  totalTorneos: number;
  nombreJugador?: string;
}

export interface StatsEquipoTorneoResponse {
  equipoId: string;
  torneoId: string;
  victorias: number;
  derrotas: number;
  partidasJugadas: number;
}

// Q7
export const getRankingEquipos = (top = 10) =>
  fetcher<RankingEquipoResponse[]>(`/api/ranking/equipos?top=${top}`);

// Q22
export const getRankingVictorias = (top = 10) =>
  fetcher<RankingVictoriasResponse[]>(`/api/ranking/victorias?top=${top}`);

// Q23
export const getRankingJugadores = (top = 10) =>
  fetcher<RankingJugadorResponse[]>(`/api/ranking/jugadores?top=${top}`);

// Q24
export const getStatsEquipoTorneo = (equipoId: string, torneoId: string) =>
  fetcher<StatsEquipoTorneoResponse>(`/api/stats/equipo/${equipoId}/torneo/${torneoId}`);
