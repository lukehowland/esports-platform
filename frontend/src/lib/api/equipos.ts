import { fetcher } from "./fetcher";

export interface EquipoResponse {
  equipoId: string;
  nombre: string;
  tag: string;
  pais: string;
  fechaCreacion: string;
}

export interface JugadorResponse {
  jugadorId: string;
  nickname: string;
  nombre: string;
  pais: string;
  rol: string;
  equipoId: string;
}

export interface CrearEquipoDto {
  nombre: string;
  tag: string;
  pais: string;
}

export interface AgregarJugadorDto {
  nickname: string;
  nombre: string;
  pais: string;
  rol: string;
}

export const getEquiposPorFecha = () =>
  fetcher<EquipoResponse[]>("/api/equipos/por-fecha");

export const getEquipoPorTag = (tag: string) =>
  fetcher<EquipoResponse>(`/api/equipos/por-tag/${encodeURIComponent(tag)}`);

export const getEquipoPorId = (id: string) =>
  fetcher<EquipoResponse>(`/api/equipos/${id}`);

export const getIntegrantesPorEquipo = (equipoId: string) =>
  fetcher<JugadorResponse[]>(`/api/equipos/${equipoId}/integrantes`);

export const getJugadoresPorEquipo = (equipoId: string, pais?: string) =>
  fetcher<JugadorResponse[]>(
    `/api/equipos/${equipoId}/jugadores${pais ? `?pais=${encodeURIComponent(pais)}` : ""}`
  );

export const getJugadorPorNickname = (nickname: string) =>
  fetcher<JugadorResponse>(`/api/jugadores/por-nickname/${encodeURIComponent(nickname)}`);

export const getJugadoresPorPais = (pais: string) =>
  fetcher<JugadorResponse[]>(`/api/jugadores/por-pais/${encodeURIComponent(pais)}`);

export const crearEquipo = (data: CrearEquipoDto) =>
  fetcher<EquipoResponse>("/api/equipos", { method: "POST", body: JSON.stringify(data) });

export const agregarJugador = (equipoId: string, data: AgregarJugadorDto) =>
  fetcher<JugadorResponse>(`/api/equipos/${equipoId}/jugadores`, {
    method: "POST",
    body: JSON.stringify(data),
  });
