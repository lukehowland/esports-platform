# Manual de Usuario — Esports Platform

## ¿Qué es esta plataforma?

Una plataforma web para **gestionar torneos de eSports**: organizadores crean torneos, los equipos se inscriben, se registran partidas, se asignan premios y se consultan rankings y estadísticas.

La arquitectura es de **microservicios distribuidos**: el frontend habla con un único API Gateway
(`:8080`) que enruta internamente a 5 servicios independientes (`teams`, `tournaments`, `matches`,
`ranking` y `auth`) sobre Apache Cassandra.

---

## Cómo iniciar el sistema

### Requisitos previos
- **Docker Desktop 4.x** (incluye Docker Compose v2)
- No se necesita Node.js, .NET ni ningún otro runtime instalado localmente

### Levantar el stack completo

```bash
git clone https://github.com/LukeHowland/esports-platform.git
cd esports-platform

# Levantar backend + frontend
docker compose up --build
```

La primera vez tarda 3–5 minutos. El seeder corre automaticamente: espera hasta ver `esports-seeder | === Seeder completado con exito ===`. Luego abrí:

- **Frontend**: http://localhost:3000
- **API Gateway (solo API)**: http://localhost:8080
- **RabbitMQ Management**: http://localhost:15672 (usuario: `guest`, contraseña: `guest`)

### Datos de ejemplo

No hay un paso manual. En cada arranque limpio, `docker compose up --build` ejecuta el seeder y
carga equipos, jugadores con historial de membresías, 5 videojuegos, 7 organizadores, torneos,
inscripciones, premios, partidas, rankings y estadísticas conectadas.

---

## Cómo iniciar sesión

La plataforma usa JWT y control de acceso basado en roles. El backend valida el token, el rol y la
entidad asociada en cada mutación.

1. Navegá a **http://localhost:3000/login** o hacé clic en **Ingresar**.
2. Usá uno de los usuarios demo sembrados:

| Rol | Usuario de ejemplo | Password | Alcance |
|---|---|---|---|
| Admin | `admin` | `admin-dev-password` | catálogos, usuarios, equipos y cualquier torneo |
| Organizador | `org_riot` | `OrgDemo2024` | solo torneos de Riot Games |
| Capitán | `cap_t1` | `CapDemo2024` | solo roster e inscripciones de T1 |
| Fan | `fan_demo` | `FanDemo2024` | solo lectura |

3. El frontend abre automáticamente el workspace del rol. Cambiar texto o selección en la interfaz
   no concede permisos: el servicio dueño vuelve a validar todo.

---

## Pantallas y funcionalidades

### Inicio (/)
Sitio público con una partida destacada T1 vs Gen.G simulada durante 30 minutos, marcador, oro,
objetivos y timeline.

### 👥 Equipos (/equipos)
- **Lista de equipos** ordenados por fecha de creación (más nuevos primero) — *Q4*
- **Buscar por tag** — *Q5*: ingresá el tag identificador (ej: `T1`, `G2`) para ir directo al equipo
- **Crear equipo** (solo Admin): nombre, tag y país

#### Detalle de equipo (/equipos/[id])
- **Integrantes** (Q6): todos los jugadores del equipo con nickname, nombre, país y rol
- **Filtrar por país** (Q3): filtrá los jugadores del equipo por su país de origen
- **Torneos** (Q14): torneos en los que participó el equipo
- **Partidas** (Q17): historial completo de partidas, con rival, torneo y resultado (VICTORIA/DERROTA)
- **Premios** (Q21): premios recibidos por el equipo
- **Estadísticas** (Q24): seleccioná un torneo para ver victorias, derrotas y partidas jugadas
- **Agregar jugador** (Capitán dueño o Admin): nickname, nombre, email, teléfono, país y rol
- **Gestión de roster**: editar contacto, liberar, fichar agentes libres y ver el historial temporal
- **Administrar equipo** (Admin): editar o eliminar un equipo sin roster; con roster devuelve `409`

### 🔍 Jugadores (/jugadores)
- **Por nickname** (Q1): buscá un jugador por su nick exacto
- **Por país** (Q2): listá todos los jugadores de un país
- **Detalle**: código `J-001`, email, teléfono, equipo activo e historial de membresías

### 🏆 Torneos (/torneos)
- **Lista por fecha** (Q12): todos los torneos ordenados por fecha de inicio
- **Buscar por código** (Q15): buscá un torneo por su código único (ej: `WORLDS25`)
- **Crear torneo** (Organizador o Admin): nombre, código único, videojuego, organizador y fechas de inicio/fin

#### Detalle de torneo (/torneos/[id])
- **Equipos inscritos** (Q13): qué equipos están participando y cuándo se inscribieron
- **Partidas** (Q16): historial cronológico de partidas del torneo con marcador
- **Premios** (Q20): premios del torneo ordenados por monto (mayor a menor)
- **Inscribir equipo** (Capitán o Admin): el capitán solo puede inscribir su propio equipo
- **Asignar premio** (Organizador dueño o Admin): monto, tipo y equipo ganador inscrito
- **Registrar partida** (Organizador dueño o Admin): equipos inscritos, marcador, ganador y fecha
- **Editar/eliminar torneo** (dueño o Admin): solo mientras no tenga inscritos ni premios

### 🎮 Videojuegos (/videojuegos)
- **Por género** (Q8): elegí el género (MOBA, FPS, BATTLE_ROYALE, etc.) y ve los videojuegos
- Cada videojuego muestra su **plataforma** (PC, Console, Mobile o Cross-platform)
- **Torneos por videojuego** (Q9): expandí un videojuego para ver sus torneos
- **Crear/editar/eliminar videojuego** (solo Admin): nombre, género y plataforma; con torneos devuelve `409`

### 🏢 Organizadores (/organizadores)
- **Lista de organizadores** (Q10)
- Cada organizador muestra su correo electrónico
- **Torneos por organizador** (Q11): expandí un organizador para ver sus torneos
- **Crear/editar/eliminar organizador** (solo Admin): nombre y email; con torneos devuelve `409`

### ⚔️ Partidas (/partidas)
- **Por fecha** (Q18): seleccioná un día para ver las partidas de esa jornada
- **Cara a cara** (Q19): seleccioná dos equipos para ver todos sus enfrentamientos directos (bidireccional — el resultado es el mismo en ambas direcciones)

### 📊 Rankings (/rankings)
- **Equipos por torneos** (Q7): Top-N de equipos con más torneos disputados
- **Por victorias** (Q22): Top-N de equipos con más victorias
- **Jugadores activos** (Q23): Top-N de jugadores por torneos disputados

> Los rankings se actualizan de forma **asíncrona** (consistencia eventual): después de inscribir un equipo o registrar una partida, pueden tardar unos segundos en reflejarse. Usá el botón **↺ Actualizar** para refrescar los datos.

---

## Géneros de videojuegos disponibles en el seeder

| Género | Ejemplos |
|---|---|
| MOBA | League of Legends, Dota 2 |
| FPS | CS2, Valorant |
| SPORTS | Rocket League |

---

## Flujos completos de ejemplo

### Como Organizador

1. Iniciá sesión como `org_riot` o `org_esl`
2. Creá un torneo para el organizador asociado a tu JWT
3. Esperá a que los capitanes inscriban sus equipos
4. Cuando empiece el torneo, registrá partidas desde el detalle del torneo
5. Asigná premios solo a equipos inscritos
6. Intentar gestionar un torneo ajeno devuelve `403`

### Como Capitán

1. Iniciá sesión como `cap_t1`
2. Agregá jugadores con sus datos de contacto
3. Liberá un jugador o fichá un agente libre por código/nickname
4. Inscribí únicamente T1 en un torneo existente
5. Seguí el progreso en Rankings y el historial de partidas

### Como Admin

1. Iniciá sesión como `admin`
2. Creá y gestioná organizadores, videojuegos, equipos y usuarios
3. Editá/eliminá entidades nuevas sin dependencias
4. Verificá que entidades con roster/torneos/inscritos respondan `409`
5. Transferí jugadores entre equipos y consultá su historial

### Como Fan

1. Accedé sin login (Fan) o seleccioná "Fan"
2. Explorá equipos, torneos, partidas y rankings libremente
3. Buscá jugadores por nickname o país

---

## Comandos útiles

```bash
# Ver logs de un servicio
docker compose logs -f frontend

# Bajar el stack. Al volver a levantar, Cassandra inicia limpia y el seeder repuebla.
docker compose down

# Arranque limpio con datos demo frescos
docker compose down
docker compose up --build

# Consola CQL de Cassandra
docker compose exec cassandra cqlsh
```

---

## Arquitectura (para la defensa)

El sistema demuestra los conceptos de **Sistemas Distribuidos**:

- **Gateway único** → el frontend habla con una sola API (:8080), el gateway YARP enruta por prefijo
- **Base por servicio** → cada servicio tiene su propio keyspace en Cassandra, sin JOINs
- **Modelo Chebotko** → las 24 tablas están diseñadas para sus queries específicas (query-first)
- **REST entre servicios** → al inscribir un equipo, `tournaments` le pregunta a `teams` el roster
- **JWT/RBAC** → `auth` emite tokens y cada microservicio valida rol y ownership
- **Event-driven con RabbitMQ** → inscripciones y partidas publican eventos; `ranking` los consume y actualiza los counters
- **Consistencia eventual** → los rankings son read-models actualizados asincrónicamente
- **BATCH en Cassandra** → toda mutación que toca varias tablas va en un BATCH atómico
