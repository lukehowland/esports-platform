# Historias de Usuario — Plataforma de eSports

> Documento de **entendimiento del producto** (no técnico). Describe qué hace la plataforma desde el punto de vista de quien la usa. Cada historia está mapeada a su query (Q1–Q24) y al endpoint que la satisface. El modelo Chebotko ordena las 24 consultas en 6 etapas (Jugadores → Equipos → Catálogos → Torneos → Partidas → Premios/Estadísticas); acá las agrupamos por servicio.

## Qué es la plataforma

Un sistema para **gestionar torneos de eSports**: organizadores crean torneos de distintos videojuegos, los equipos se inscriben con su plantel de jugadores, se juegan partidas, se reparten premios, y existen rankings y estadísticas. Multiplataforma (varios videojuegos) y pensada para escalar (Cassandra + microservicios).

## Actores

- **Organizador** — crea torneos y define premios/partidas de sus torneos.
- **Capitán / Equipo** — registra su equipo y jugadores; inscribe el equipo en torneos.
- **Jugador** — pertenece a un equipo; tiene nick, país, rol.
- **Fan / Visitante** — consulta equipos, torneos, partidas, rankings, premios.
- **Admin demo** — usuario técnico del seed y pruebas; puede ejecutar mutaciones de setup.
- **Sistema** — actor no humano: reacciona a eventos (actualizar rankings y stats).

---

## Épica 1 — Jugadores y equipos (servicio teams)

**HU-01 — Registrar un equipo**
Como **capitán**, quiero registrar mi equipo (nombre, tag, país), para participar en torneos.
- *Endpoint:* `POST /api/equipos`.

**HU-02 — Agregar jugadores a mi equipo**
Como **capitán**, quiero agregar jugadores (nick, nombre, país, rol) a mi equipo, para armar mi plantel.
- *Endpoint:* `POST /api/equipos/{equipoId}/jugadores`.

**HU-03 — Buscar un jugador por su nickname**
Como **fan**, quiero buscar un jugador por su nick exacto, para encontrarlo rápido.
- *Query:* **Q1** · *Endpoint:* `GET /api/jugadores/por-nickname/{nickname}`.

**HU-04 — Ver jugadores de un país**
Como **fan**, quiero ver los jugadores registrados de un país, para seguir el talento local.
- *Query:* **Q2** · *Endpoint:* `GET /api/jugadores/por-pais/{pais}`.

**HU-05 — Ver los jugadores de un equipo, filtrando por país**
Como **fan** o **capitán**, quiero ver los jugadores de un equipo y poder filtrarlos por país.
- *Query:* **Q3** · *Endpoint:* `GET /api/equipos/{equipoId}/jugadores?pais={pais}`.

**HU-06 — Listar equipos por fecha de creación**
Como **fan**, quiero ver los equipos ordenados por fecha de creación (más nuevos primero), para descubrir los recién llegados.
- *Query:* **Q4** · *Endpoint:* `GET /api/equipos/por-fecha`.

**HU-07 — Buscar un equipo por su tag**
Como **fan**, quiero buscar un equipo por su tag identificador (ej. "TIG"), para llegar directo a él.
- *Query:* **Q5** · *Endpoint:* `GET /api/equipos/por-tag/{tag}`.

**HU-08 — Ver el roster completo de un equipo**
Como **fan** o **organizador**, quiero ver todos los integrantes de un equipo, para conocer su composición.
- *Query:* **Q6** · *Endpoint:* `GET /api/equipos/{equipoId}/integrantes`.

---

## Épica 2 — Catálogos, torneos, inscripciones y premios (servicio tournaments)

**HU-09 — Registrar un videojuego**
Como **admin**, quiero registrar un videojuego con su género, para mantener limpio el catálogo global disponible para torneos.
- *Endpoint:* `POST /api/videojuegos`.

**HU-10 — Ver videojuegos por género**
Como **fan**, quiero ver los videojuegos disponibles de un género (ej. MOBA, FPS), para explorar el catálogo.
- *Query:* **Q8** · *Endpoint:* `GET /api/videojuegos/por-genero/{genero}`.

**HU-11 — Registrar un organizador**
Como **organizador**, quiero registrarme en la plataforma, para poder crear torneos a mi nombre.
- *Endpoint:* `POST /api/organizadores`.

**HU-12 — Ver todos los organizadores**
Como **fan**, quiero ver la lista de organizadores registrados, para saber quién organiza torneos.
- *Query:* **Q10** · *Endpoint:* `GET /api/organizadores`.

**HU-13 — Crear un torneo**
Como **organizador**, quiero crear un torneo (videojuego, fecha, código único), para abrir la competencia.
- *Endpoint:* `POST /api/torneos`.

**HU-14 — Ver torneos de un videojuego**
Como **fan**, quiero ver los torneos de un videojuego ordenados por fecha, para seguir mi juego favorito.
- *Query:* **Q9** · *Endpoint:* `GET /api/videojuegos/{videojuegoId}/torneos`.

**HU-15 — Ver torneos de un organizador**
Como **fan**, quiero ver los torneos de un organizador ordenados por fecha.
- *Query:* **Q11** · *Endpoint:* `GET /api/organizadores/{organizadorId}/torneos`.

**HU-16 — Ver torneos por fecha de inicio**
Como **fan**, quiero ver todos los torneos ordenados por fecha de inicio, para saber qué se viene.
- *Query:* **Q12** · *Endpoint:* `GET /api/torneos/por-fecha`.

**HU-17 — Buscar un torneo por código**
Como **fan** o **capitán**, quiero buscar un torneo por su código único, para llegar directo a inscribirme.
- *Query:* **Q15** · *Endpoint:* `GET /api/torneos/por-codigo/{codigo}`.

**HU-18 — Inscribir mi equipo en un torneo**
Como **capitán**, quiero inscribir mi equipo en un torneo, para competir.
- *Criterios:* queda registrado en ambos sentidos (equipo↔torneo) y dispara la actualización de rankings.
- *Endpoint:* `POST /api/torneos/{torneoId}/inscripciones`.

**HU-19 — Ver los equipos inscritos en un torneo**
Como **fan** u **organizador**, quiero ver qué equipos están inscritos en un torneo.
- *Query:* **Q13** · *Endpoint:* `GET /api/torneos/{torneoId}/equipos`.

**HU-20 — Ver los torneos en los que participó un equipo**
Como **fan** o **capitán**, quiero ver los torneos de un equipo ordenados por fecha reciente.
- *Query:* **Q14** · *Endpoint:* `GET /api/torneos/por-equipo/{equipoId}`.

**HU-21 — Definir premios de un torneo**
Como **organizador**, quiero asignar premios (monto, tipo, opcionalmente al equipo ganador), para premiar la competencia.
- *Endpoint:* `POST /api/torneos/{torneoId}/premios`.

**HU-22 — Ver los premios de un torneo (mayor a menor)**
Como **fan**, quiero ver los premios de un torneo ordenados por monto, para saber qué hay en juego.
- *Query:* **Q20** · *Endpoint:* `GET /api/torneos/{torneoId}/premios`.

**HU-23 — Ver los premios recibidos por un equipo**
Como **fan** o **capitán**, quiero ver el historial de premios de un equipo, para conocer su palmarés.
- *Query:* **Q21** · *Endpoint:* `GET /api/premios/por-equipo/{equipoId}`.

---

## Épica 3 — Partidas (servicio matches)

**HU-24 — Registrar una partida**
Como **organizador**, quiero registrar una partida entre dos equipos con su resultado y ganador, para llevar el historial.
- *Criterios:* queda asociada al torneo, al historial de **ambos** equipos y al de enfrentamientos directos; dispara la actualización de victorias y stats.
- *Endpoint:* `POST /api/partidas`.

**HU-25 — Ver las partidas de un torneo (cronológico)**
Como **fan**, quiero ver las partidas de un torneo en orden cronológico, para seguir su desarrollo.
- *Query:* **Q16** · *Endpoint:* `GET /api/partidas/por-torneo/{torneoId}`.

**HU-25A — Ver una partida destacada en vivo**
Como **visitante**, quiero ver una partida destacada simulada con marcador, oro y objetivos en tiempo real, para entender inmediatamente que la plataforma cubre experiencia de torneo en curso.
- *Criterios:* la partida dura 30 minutos, inicia 0-0, aumenta oro de ambos equipos y al minuto 5 registra dragon para T1. La simulación cuenta una remontada: Gen.G domina los primeros 5 minutos (0-2 en kills) y T1 da la vuelta al marcador con dragones, Heraldo, Barón y torres hasta cerrar la partida.
- *Endpoint:* `GET /api/partidas/en-vivo/destacada`.

**HU-26 — Ver el historial de partidas de un equipo**
Como **fan** o **capitán**, quiero ver todas las partidas que jugó un equipo, para analizar su desempeño.
- *Query:* **Q17** · *Endpoint:* `GET /api/partidas/por-equipo/{equipoId}`.

**HU-27 — Ver las partidas de un día**
Como **fan**, quiero ver las partidas jugadas en una fecha específica, para ponerme al día de esa jornada.
- *Query:* **Q18** · *Endpoint:* `GET /api/partidas/por-fecha/{dia}` (`YYYY-MM-DD`).

**HU-28 — Ver los enfrentamientos directos entre dos equipos**
Como **fan**, quiero ver todas las partidas entre dos equipos (sin importar quién fue local), para conocer su historial cara a cara.
- *Query:* **Q19** · *Endpoint:* `GET /api/partidas/entre/{equipoId}/{rivalId}`.

---

## Épica 4 — Rankings y estadísticas (servicio ranking, event-driven)

**HU-29 — Actualizar rankings automáticamente (Sistema)**
Como **sistema**, cuando un equipo se inscribe o se juega una partida, quiero actualizar los rankings y estadísticas sin intervención manual.
- *Criterios:* reacciona a `TeamRegisteredToTournament` (torneos del equipo y de sus jugadores) y a `MatchPlayed` (victorias y stats por torneo); los cambios se reflejan poco después (consistencia eventual).
- *Mecanismo:* consumidores de eventos (no son endpoints públicos).

**HU-30 — Ver el ranking global de equipos por torneos**
Como **fan**, quiero ver el Top-N de equipos por cantidad de torneos disputados, para conocer a los más activos.
- *Query:* **Q7** · *Endpoint:* `GET /api/ranking/equipos?top={n}`.

**HU-31 — Ver el ranking de equipos por victorias**
Como **fan**, quiero ver el Top-N de equipos por victorias totales, para saber quiénes ganan más.
- *Query:* **Q22** · *Endpoint:* `GET /api/ranking/victorias?top={n}`.

**HU-32 — Ver los jugadores más activos**
Como **fan**, quiero ver el Top-N de jugadores por torneos disputados, para seguir a los más constantes.
- *Query:* **Q23** · *Endpoint:* `GET /api/ranking/jugadores?top={n}`.

**HU-33 — Ver las estadísticas de un equipo en un torneo**
Como **fan** o **capitán**, quiero ver las estadísticas de un equipo en un torneo (victorias, derrotas, partidas jugadas), para evaluar su rendimiento.
- *Query:* **Q24** · *Endpoint:* `GET /api/stats/equipo/{equipoId}/torneo/{torneoId}`.

---

## Épica 5 — Identidad y permisos (servicio auth)

**HU-34 — Iniciar sesión con un rol demo**
Como **organizador**, **capitán**, **fan** o **admin demo**, quiero iniciar sesión, para que el backend sepa qué puedo hacer.
- *Endpoint:* `POST /api/auth/login`.

**HU-35 — Consultar mi perfil autenticado**
Como **usuario autenticado**, quiero ver mi rol y entidad asociada, para que el frontend adapte la experiencia.
- *Endpoint:* `GET /api/auth/me`.

**HU-36 — Bloquear mutaciones sin permiso**
Como **sistema**, quiero rechazar mutaciones sin token (`401`) o con rol/ownership incorrecto (`403`), para que un organizador no actúe como otro organizador y un capitán no actúe por otro equipo.
- *Criterios:* lecturas públicas siguen funcionando; mutaciones se protegen en el servicio dueño del dominio.

---

## Tabla resumen: historia ↔ query ↔ endpoint

| HU | Query | Endpoint | Servicio |
|---|---|---|---|
| HU-01 | — | `POST /api/equipos` | teams |
| HU-02 | — | `POST /api/equipos/{id}/jugadores` | teams |
| HU-03 | Q1 | `GET /api/jugadores/por-nickname/{nickname}` | teams |
| HU-04 | Q2 | `GET /api/jugadores/por-pais/{pais}` | teams |
| HU-05 | Q3 | `GET /api/equipos/{id}/jugadores?pais=` | teams |
| HU-06 | Q4 | `GET /api/equipos/por-fecha` | teams |
| HU-07 | Q5 | `GET /api/equipos/por-tag/{tag}` | teams |
| HU-08 | Q6 | `GET /api/equipos/{id}/integrantes` | teams |
| HU-09 | — | `POST /api/videojuegos` | tournaments |
| HU-10 | Q8 | `GET /api/videojuegos/por-genero/{genero}` | tournaments |
| HU-11 | — | `POST /api/organizadores` | tournaments |
| HU-12 | Q10 | `GET /api/organizadores` | tournaments |
| HU-13 | — | `POST /api/torneos` | tournaments |
| HU-14 | Q9 | `GET /api/videojuegos/{id}/torneos` | tournaments |
| HU-15 | Q11 | `GET /api/organizadores/{id}/torneos` | tournaments |
| HU-16 | Q12 | `GET /api/torneos/por-fecha` | tournaments |
| HU-17 | Q15 | `GET /api/torneos/por-codigo/{codigo}` | tournaments |
| HU-18 | — | `POST /api/torneos/{id}/inscripciones` | tournaments |
| HU-19 | Q13 | `GET /api/torneos/{id}/equipos` | tournaments |
| HU-20 | Q14 | `GET /api/torneos/por-equipo/{id}` | tournaments |
| HU-21 | — | `POST /api/torneos/{id}/premios` | tournaments |
| HU-22 | Q20 | `GET /api/torneos/{id}/premios` | tournaments |
| HU-23 | Q21 | `GET /api/premios/por-equipo/{id}` | tournaments |
| HU-24 | — | `POST /api/partidas` | matches |
| HU-25A | — | `GET /api/partidas/en-vivo/destacada` | matches |
| HU-25 | Q16 | `GET /api/partidas/por-torneo/{id}` | matches |
| HU-26 | Q17 | `GET /api/partidas/por-equipo/{id}` | matches |
| HU-27 | Q18 | `GET /api/partidas/por-fecha/{dia}` | matches |
| HU-28 | Q19 | `GET /api/partidas/entre/{id}/{rivalId}` | matches |
| HU-29 | — | (consumidores de eventos) | ranking |
| HU-30 | Q7 | `GET /api/ranking/equipos?top=` | ranking |
| HU-31 | Q22 | `GET /api/ranking/victorias?top=` | ranking |
| HU-32 | Q23 | `GET /api/ranking/jugadores?top=` | ranking |
| HU-33 | Q24 | `GET /api/stats/equipo/{id}/torneo/{id}` | ranking |
| HU-34 | — | `POST /api/auth/login` | auth |
| HU-35 | — | `GET /api/auth/me` | auth |
| HU-36 | — | mutaciones protegidas con `401`/`403` | teams/tournaments/matches |

## Para la defensa: cómo contar el "por qué" distribuido

- **Gateway** → "el frontend habla con una sola API aunque atrás haya 4 servicios de negocio y auth" (todas las lecturas pasan por acá).
- **Base por servicio** → "cada servicio es autónomo; por eso desnormalizamos con Cassandra/Chebotko en vez de hacer JOINs".
- **REST entre servicios** → HU-18: "al inscribir, tournaments le pide a teams el nombre y el roster del equipo".
- **Event-driven + CQRS** → HU-18, HU-24, HU-29: "inscripciones y partidas publican eventos; el servicio de ranking es un read-model que reacciona y agrega métricas con counters; los rankings son eventualmente consistentes, que es el comportamiento esperado de un sistema distribuido".
- **Los fixes** → demuestran que entienden Cassandra de verdad: counters porque la primary key es inmutable (Q7/Q22/Q23), doble escritura bidireccional para enfrentamientos (Q19), y partición por día para no fragmentar (Q18).
