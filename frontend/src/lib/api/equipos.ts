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
  codigo: string;
  nickname: string;
  nombre: string;
  pais: string;
  rol: string;
  email: string;
  telefono: string;
  equipoId: string | null;
}

// RF-03: una entrada del historial de equipos del jugador (activa = sin fechaHasta).
export interface MembresiaResponse {
  equipoId: string;
  nombreEquipo: string;
  tag: string;
  rol: string;
  fechaDesde: string;
  fechaHasta: string | null;
  activa: boolean;
}

export interface CrearEquipoDto {
  nombre: string;
  tag: string;
  pais: string;
}

export type EditarEquipoDto = CrearEquipoDto;

export interface AgregarJugadorDto {
  nickname: string;
  nombre: string;
  pais: string;
  rol: string;
  email: string;
  telefono: string;
}

export interface EditarJugadorDto {
  nombre: string;
  email: string;
  telefono: string;
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

// RF-03: jugador por id / código + historial de equipos
export const getJugador = (id: string) =>
  fetcher<JugadorResponse>(`/api/jugadores/${id}`);

export const getJugadorPorCodigo = (codigo: string) =>
  fetcher<JugadorResponse>(`/api/jugadores/por-codigo/${encodeURIComponent(codigo)}`);

export const getMembresiasJugador = (id: string) =>
  fetcher<MembresiaResponse[]>(`/api/jugadores/${id}/membresias`);

// RF-03: liberar (baja a agente libre) y asignar/fichar/transferir a un equipo
export const liberarJugador = (id: string) =>
  fetcher<void>(`/api/jugadores/${id}/liberar`, { method: "POST" });

export const asignarJugador = (id: string, data: { equipoDestinoId: string; rol?: string }) =>
  fetcher<void>(`/api/jugadores/${id}/asignar`, { method: "POST", body: JSON.stringify(data) });

export const crearEquipo = (data: CrearEquipoDto) =>
  fetcher<EquipoResponse>("/api/equipos", { method: "POST", body: JSON.stringify(data) });

// RF-02: editar / eliminar equipo (admin; bloqueado si tiene roster)
export const editarEquipo = (equipoId: string, data: EditarEquipoDto) =>
  fetcher<EquipoResponse>(`/api/equipos/${equipoId}`, { method: "PUT", body: JSON.stringify(data) });

export const eliminarEquipo = (equipoId: string) =>
  fetcher<void>(`/api/equipos/${equipoId}`, { method: "DELETE" });

// RF-01: editar / eliminar jugador
export const editarJugador = (jugadorId: string, data: EditarJugadorDto) =>
  fetcher<JugadorResponse>(`/api/jugadores/${jugadorId}`, { method: "PUT", body: JSON.stringify(data) });

export const eliminarJugador = (jugadorId: string) =>
  fetcher<void>(`/api/jugadores/${jugadorId}`, { method: "DELETE" });

export const agregarJugador = (equipoId: string, data: AgregarJugadorDto) =>
  fetcher<JugadorResponse>(`/api/equipos/${equipoId}/jugadores`, {
    method: "POST",
    body: JSON.stringify(data),
  });
