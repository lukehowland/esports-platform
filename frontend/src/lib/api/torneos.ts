import { fetcher } from "./fetcher";

export interface TorneoResponse {
  torneoId: string;
  nombre: string;
  codigo: string;
  videojuegoId: string;
  nombreVideojuego: string;
  organizadorId: string;
  nombreOrganizador: string;
  fechaInicio: string;
}

export interface TorneoResumenResponse {
  torneoId: string;
  nombreTorneo: string;
  nombreVideojuego: string;
  fechaInicio: string;
}

export interface TorneoPorCodigoResponse {
  torneoId: string;
  nombre: string;
  fechaInicio: string;
}

export interface TorneoPorVideojuegoResponse {
  torneoId: string;
  nombreTorneo: string;
  nombreOrganizador: string;
  fechaInicio: string;
}

export interface EquipoPorTorneoResponse {
  equipoId: string;
  nombreEquipo: string;
  fechaInscripcion: string;
}

export interface TorneoPorEquipoResponse {
  torneoId: string;
  nombreTorneo: string;
  nombreVideojuego: string;
  fechaInicio: string;
}

export interface PremioResponse {
  premioId: string;
  torneoId: string;
  monto: number;
  tipo: string;
  equipoId?: string;
  nombreEquipo?: string;
}

export interface PremioEquipoResponse {
  premioId: string;
  torneoId: string;
  nombreTorneo: string;
  monto: number;
  tipo: string;
}

export interface OrganizadorResponse {
  organizadorId: string;
  nombre: string;
}

export interface VideojuegoPorGeneroResponse {
  videojuegoId: string;
  nombre: string;
}

export interface VideojuegoResponse {
  videojuegoId: string;
  nombre: string;
  genero: string;
}

export interface CrearTorneoDto {
  nombre: string;
  codigo: string;
  videojuegoId: string;
  organizadorId: string;
  fechaInicio: string;
}

export interface CrearVideojuegoDto {
  nombre: string;
  genero: string;
}

export interface CrearOrganizadorDto {
  nombre: string;
}

export interface AsignarPremioDto {
  monto: number;
  tipo: string;
  equipoId?: string;
}

// Q8
export const getVideojuegosPorGenero = (genero: string) =>
  fetcher<VideojuegoPorGeneroResponse[]>(`/api/videojuegos/por-genero/${encodeURIComponent(genero)}`);

// Q9
export const getTorneosPorVideojuego = (videojuegoId: string) =>
  fetcher<TorneoPorVideojuegoResponse[]>(`/api/videojuegos/${videojuegoId}/torneos`);

// Q10
export const getOrganizadores = () =>
  fetcher<OrganizadorResponse[]>("/api/organizadores");

// Q11
export const getTorneosPorOrganizador = (organizadorId: string) =>
  fetcher<TorneoResumenResponse[]>(`/api/organizadores/${organizadorId}/torneos`);

// Q12
export const getTorneosPorFecha = () =>
  fetcher<TorneoResumenResponse[]>("/api/torneos/por-fecha");

// Q13
export const getEquiposPorTorneo = (torneoId: string) =>
  fetcher<EquipoPorTorneoResponse[]>(`/api/torneos/${torneoId}/equipos`);

// Q14
export const getTorneosPorEquipo = (equipoId: string) =>
  fetcher<TorneoPorEquipoResponse[]>(`/api/torneos/por-equipo/${equipoId}`);

// Q15
export const getTorneoPorCodigo = (codigo: string) =>
  fetcher<TorneoPorCodigoResponse>(`/api/torneos/por-codigo/${encodeURIComponent(codigo)}`);

// Q20
export const getPremiosPorTorneo = (torneoId: string) =>
  fetcher<PremioResponse[]>(`/api/torneos/${torneoId}/premios`);

// Q21
export const getPremiosPorEquipo = (equipoId: string) =>
  fetcher<PremioEquipoResponse[]>(`/api/premios/por-equipo/${equipoId}`);

// GET by id
export const getTorneoPorId = (torneoId: string) =>
  fetcher<TorneoResponse>(`/api/torneos/${torneoId}`);

// Mutaciones
export const crearVideojuego = (data: CrearVideojuegoDto) =>
  fetcher<VideojuegoResponse>("/api/videojuegos", { method: "POST", body: JSON.stringify(data) });

export const crearOrganizador = (data: CrearOrganizadorDto) =>
  fetcher<OrganizadorResponse>("/api/organizadores", { method: "POST", body: JSON.stringify(data) });

export const crearTorneo = (data: CrearTorneoDto) =>
  fetcher<TorneoResponse>("/api/torneos", { method: "POST", body: JSON.stringify(data) });

export const inscribirEquipo = (torneoId: string, equipoId: string) =>
  fetcher<unknown>(`/api/torneos/${torneoId}/inscripciones`, {
    method: "POST",
    body: JSON.stringify({ equipoId }),
  });

export const asignarPremio = (torneoId: string, data: AsignarPremioDto) =>
  fetcher<PremioResponse>(`/api/torneos/${torneoId}/premios`, {
    method: "POST",
    body: JSON.stringify(data),
  });

// GET videojuego por id (trae nombre + género)
export const getVideojuegoPorId = (videojuegoId: string) =>
  fetcher<VideojuegoResponse>(`/api/videojuegos/${videojuegoId}`);

// Edición / eliminación de organizadores (admin; bloqueado si tiene torneos)
export const editarOrganizador = (organizadorId: string, data: CrearOrganizadorDto) =>
  fetcher<OrganizadorResponse>(`/api/organizadores/${organizadorId}`, {
    method: "PUT",
    body: JSON.stringify(data),
  });

export const eliminarOrganizador = (organizadorId: string) =>
  fetcher<void>(`/api/organizadores/${organizadorId}`, { method: "DELETE" });

// Edición / eliminación de videojuegos (admin; bloqueado si tiene torneos)
export const editarVideojuego = (videojuegoId: string, data: CrearVideojuegoDto) =>
  fetcher<VideojuegoResponse>(`/api/videojuegos/${videojuegoId}`, {
    method: "PUT",
    body: JSON.stringify(data),
  });

export const eliminarVideojuego = (videojuegoId: string) =>
  fetcher<void>(`/api/videojuegos/${videojuegoId}`, { method: "DELETE" });
