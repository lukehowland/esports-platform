# Manual de Usuario — Esports Platform

## ¿Qué es esta plataforma?

Una plataforma web para **gestionar torneos de eSports**: organizadores crean torneos, los equipos se inscriben, se registran partidas, se asignan premios y se consultan rankings y estadísticas.

La arquitectura es de **microservicios distribuidos**: el frontend habla con un único API Gateway (`:8080`) que enruta internamente a 4 servicios independientes sobre Apache Cassandra.

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

No hay un paso manual. En cada arranque limpio, `docker compose up --build` ejecuta el seeder y carga datos conectados para toda la plataforma: 40 equipos, 5 videojuegos, 7 organizadores, 12 torneos, inscripciones, premios, partidas, rankings y estadisticas.

---

## Cómo "iniciar sesión"

La plataforma usa un sistema de **roles basado en identidad** (sin contraseñas):

1. Navegá a **http://localhost:3000/login** o hacé clic en **"Ingresar"** en la barra de navegación.
2. Elegí tu rol:
   - **Organizador**: podés crear videojuegos, organizadores, torneos, asignar premios y registrar partidas. Se te pide elegir qué organizador representás (de los registrados en el sistema).
   - **Capitán**: podés crear tu equipo, agregar jugadores e inscribir tu equipo en torneos. Se te pide elegir qué equipo liderás.
   - **Fan / Visitante**: acceso de solo lectura a toda la información.
3. Hacé clic en **"Ingresar"**.

> **Nota**: Esta autenticación es de demostración. No hay contraseñas reales; el backend no valida roles. Es una decisión de diseño para el examen de Sistemas Distribuidos, donde el foco es la arquitectura distribuida, no la seguridad.

---

## Pantallas y funcionalidades

### 🏠 Inicio (/)
Descripción general de la plataforma y diagrama de la arquitectura de microservicios.

### 👥 Equipos (/equipos)
- **Lista de equipos** ordenados por fecha de creación (más nuevos primero) — *Q4*
- **Buscar por tag** — *Q5*: ingresá el tag identificador (ej: `T1`, `G2`) para ir directo al equipo
- **Crear equipo** (solo Capitán): nombre, tag único y país

#### Detalle de equipo (/equipos/[id])
- **Integrantes** (Q6): todos los jugadores del equipo con nickname, nombre, país y rol
- **Filtrar por país** (Q3): filtrá los jugadores del equipo por su país de origen
- **Torneos** (Q14): torneos en los que participó el equipo
- **Partidas** (Q17): historial completo de partidas, con rival, torneo y resultado (VICTORIA/DERROTA)
- **Premios** (Q21): premios recibidos por el equipo
- **Estadísticas** (Q24): seleccioná un torneo para ver victorias, derrotas y partidas jugadas
- **Agregar jugador** (solo Capitán dueño del equipo): nickname, nombre, país y rol

### 🔍 Jugadores (/jugadores)
- **Por nickname** (Q1): buscá un jugador por su nick exacto
- **Por país** (Q2): listá todos los jugadores de un país

### 🏆 Torneos (/torneos)
- **Lista por fecha** (Q12): todos los torneos ordenados por fecha de inicio
- **Buscar por código** (Q15): buscá un torneo por su código único (ej: `WORLDS25`)
- **Crear torneo** (solo Organizador): nombre, código único, videojuego, organizador y fecha de inicio

#### Detalle de torneo (/torneos/[id])
- **Equipos inscritos** (Q13): qué equipos están participando y cuándo se inscribieron
- **Partidas** (Q16): historial cronológico de partidas del torneo con marcador
- **Premios** (Q20): premios del torneo ordenados por monto (mayor a menor)
- **Inscribir equipo** (solo Capitán): inscribí tu equipo en el torneo
- **Asignar premio** (solo Organizador): monto, tipo y (opcionalmente) equipo ganador
- **Registrar partida** (solo Organizador): equipos, marcador, ganador y fecha

### 🎮 Videojuegos (/videojuegos)
- **Por género** (Q8): elegí el género (MOBA, FPS, BATTLE_ROYALE, etc.) y ve los videojuegos
- **Torneos por videojuego** (Q9): expandí un videojuego para ver sus torneos
- **Crear videojuego** (solo Organizador): nombre y género

### 🏢 Organizadores (/organizadores)
- **Lista de organizadores** (Q10)
- **Torneos por organizador** (Q11): expandí un organizador para ver sus torneos
- **Crear organizador** (solo Organizador): nombre del organizador

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

1. Login → Organizador → elegí "ESL Gaming"
2. Creá un videojuego: *Valorant* / género *FPS*
3. Creá un torneo: *VALORANT CUP 2026*, código *VCUP26*, videojuego *Valorant*, fecha futura
4. Esperá a que los capitanes inscriban sus equipos
5. Cuando empiece el torneo, registrá partidas desde el detalle del torneo
6. Asigná premios a los ganadores

### Como Capitán

1. Login → Capitán → elegí tu equipo (ej: "T1")
2. Agregá jugadores a tu equipo desde el detalle del equipo
3. Inscribí tu equipo en un torneo existente desde el detalle del torneo
4. Seguí el progreso en Rankings y el historial de partidas de tu equipo

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
- **Event-driven con RabbitMQ** → inscripciones y partidas publican eventos; `ranking` los consume y actualiza los counters
- **Consistencia eventual** → los rankings son read-models actualizados asincrónicamente
- **BATCH en Cassandra** → toda mutación que toca varias tablas va en un BATCH atómico
