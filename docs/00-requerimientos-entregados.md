# Requerimientos entregados y trazabilidad

Fuente: `Sistematorneodeesaports.pdf`, documento entregado antes de la implementación.

Este archivo conserva los requerimientos originales y registra dónde se demuestran en el sistema.
Las consultas Q1-Q24 siguen documentadas en `docs/02-modelo-datos.md` y
`docs/04-contratos-api.md`.

## Estado funcional

| RF | Implementación verificable | Superficie principal |
|---|---|---|
| RF-01 | Alta, edición y eliminación controlada de jugadores; nombre, nickname, email, teléfono y país | `/mi-equipo`, `/panel/equipos/{id}`, `/jugadores/{id}` |
| RF-02 | Alta, edición y eliminación controlada de equipos; nombre, tag y fecha de creación | `/panel/equipos`, `/panel/equipos/{id}` |
| RF-03 | Membresías N:N temporales; liberar, fichar y transferir, con una sola membresía activa | `/mi-equipo`, `/panel/equipos/{id}`, `/jugadores/{id}` |
| RF-04 | Alta y gestión de videojuegos con nombre, género y plataforma | `/panel/videojuegos`, `/videojuegos` |
| RF-05 | Alta y gestión de organizadores con nombre y correo electrónico | `/panel/organizadores`, `/organizadores` |
| RF-06 | Alta y gestión de torneos con código, nombre, fecha de inicio/fin, videojuego y organizador | `/panel/crear-torneo`, `/panel/torneos/{id}`, `/torneos/{id}` |
| RF-07 | Inscripción N:N equipo-torneo con autorización del capitán o admin | detalle de torneo y `POST /api/torneos/{id}/inscripciones` |
| RF-08 | Registro de partidas con fecha, participantes, resultado y ganador | `/panel/torneos/{id}` |
| RF-09 | Registro de premios por monto y tipo, opcionalmente vinculados a un equipo inscrito | `/panel/torneos/{id}` |
| RF-10 | Rankings, resultados y estadísticas Q7/Q16-Q24 | `/rankings`, `/partidas`, detalles de equipo/torneo |
| RF-11 | JWT y RBAC en backend para todas las mutaciones administrativas | `/login`, `auth` y políticas de cada microservicio |

## Reglas de integridad defendibles

- Un jugador puede pertenecer a varios equipos a lo largo del tiempo, pero solo a uno de forma activa.
- El código `J-001` del jugador es legible e inmutable.
- Las mutaciones que actualizan varias tablas desnormalizadas usan `BATCH`.
- No se editan claves primarias de Cassandra: los cambios de tag/género se hacen con
  `DELETE` + `INSERT` en el índice correspondiente.
- Equipos con roster, torneos con inscritos/premios y catálogos con torneos se bloquean con `409`.
- Las funciones administrativas requieren JWT y validan rol y ownership en el servicio dueño.

# 1. Lista de Requerimientos

Los requerimientos se clasifican en funcionales y no funcionales, priorizados por criticidad (Alta / Media / Baja). Toda entidad presente en el Modelo Entidad-Relación tiene correspondencia directa con al menos un requerimiento funcional.

---

## 1.1 Requerimientos Funcionales

| ID    | Descripción                                                                                                                              | Prioridad | Entidad(es) relacionada(s)              |
|-------|------------------------------------------------------------------------------------------------------------------------------------------|-----------|-----------------------------------------|
| RF-01 | El sistema debe permitir registrar y gestionar jugadores, almacenando nombre, nickname, email, teléfono y país de origen.                | Alta      | JUGADOR                                 |
| RF-02 | El sistema debe permitir registrar y gestionar equipos, almacenando nombre, tag identificador y fecha de creación.                       | Alta      | EQUIPO                                  |
| RF-03 | El sistema debe permitir asignar uno o más jugadores a uno o más equipos (relación N:N).                                                 | Alta      | JUGADOR, EQUIPO                         |
| RF-04 | El sistema debe permitir registrar videojuegos disponibles, indicando nombre, género y plataforma.                                       | Alta      | VIDEOJUEGO                              |
| RF-05 | El sistema debe permitir registrar y gestionar organizadores de torneos, con nombre y correo electrónico.                                | Alta      | ORGANIZADOR                             |
| RF-06 | El sistema debe permitir crear torneos indicando código, nombre, fechas y el videojuego al que corresponde, vinculado a un organizador.  | Alta      | TORNEO, VIDEOJUEGO, ORGANIZADOR         |
| RF-07 | El sistema debe permitir inscribir uno o más equipos en un torneo (relación N:N).                                                        | Alta      | EQUIPO, TORNEO                          |
| RF-08 | El sistema debe permitir registrar las partidas generadas dentro de un torneo, incluyendo fecha y resultado.                             | Media     | TORNEO, PARTIDA                         |
| RF-09 | El sistema debe permitir registrar los premios otorgados por un torneo, especificando monto y tipo.                                      | Media     | TORNEO, PREMIO                          |
| RF-10 | El sistema debe permitir consultar clasificaciones, resultados de partidas y estadísticas de torneos.                                    | Media     | PARTIDA, EQUIPO                         |
| RF-11 | El sistema debe requerir autenticación para acceso a las funciones de administración.                                                    | Alta      | —                                       |

---

## 1.2 Requerimientos No Funcionales

| ID     | Descripción                                                                                                                                           | Prioridad |
|--------|-------------------------------------------------------------------------------------------------------------------------------------------------------|-----------|
| RNF-01 | **Rendimiento:** el sistema debe responder a consultas estándar en menos de 2 segundos bajo carga normal.                                             | Alta      |
| RNF-02 | **Usabilidad:** la interfaz debe ser intuitiva y accesible desde cualquier navegador web moderno sin instalación adicional.                            | Alta      |
| RNF-03 | **Seguridad:** los datos de jugadores y organizadores deben estar protegidos mediante cifrado en tránsito (HTTPS) y control de acceso basado en roles. | Alta      |
| RNF-04 | **Disponibilidad:** el sistema debe garantizar una disponibilidad del 99 % durante el horario activo de torneos.                                      | Media     |
| RNF-05 | **Escalabilidad:** la base de datos relacional debe soportar al menos 10 000 jugadores y 500 torneos registrados sin degradación perceptible.          | Media     |

---

# 2. Modelo Entidad-Relación (MER)

Diagrama conceptual del sistema en notación Chen. Se identifican 7 entidades, sus atributos clave (llaves primarias subrayadas) y 6 relaciones cardinalizadas.

> **Figura 1.** Diagrama Entidad-Relación — Sistema de Gestión de Torneos de eSports. Notación Chen. 7 entidades · 6 relaciones · Llaves primarias subrayadas.

---

## 2.1 Entidades y Relaciones

| Entidad     | PK | Atributos                            | Relaciones en el MER                                                                                                         |
|-------------|----|--------------------------------------|------------------------------------------------------------------------------------------------------------------------------|
| JUGADOR     | ID | NOMBRE, NICKNAME, EMAIL, TEL, PAIS   | PERTENECE con EQUIPO (N:N)                                                                                                   |
| EQUIPO      | ID | NOMBRE, TAG, F.CREACION              | PERTENECE con JUGADOR (N:N) · PARTICIPA con TORNEO (N:N)                                                                     |
| TORNEO      | ID | CODIGO, NOMBRE, FECHA                | PARTICIPA con EQUIPO (N:N) · USA VIDEOJUEGO (N:1) · ORGANIZA por ORGANIZADOR (N:1) · GENERA PARTIDAS (1:N) · OTORGA PREMIOS (1:N) |
| VIDEOJUEGO  | ID | NOMBRE, GENERO, PLATAFORMA           | USA en TORNEO (1:N)                                                                                                          |
| ORGANIZADOR | ID | NOMBRE, EMAIL                        | ORGANIZA TORNEO (1:N)                                                                                                        |
| PARTIDA     | ID | FECHA, RESULTADO                     | GENERA desde TORNEO (N:1)                                                                                                    |
| PREMIO      | ID | MONTO, TIPO                          | OTORGA desde TORNEO (N:1)                                                                                                    |
