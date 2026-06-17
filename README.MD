# Esports Platform — Backend

Backend de gestión de torneos de eSports. Arquitectura de microservicios .NET 10 sobre Apache Cassandra, con mensajería RabbitMQ y API Gateway YARP.

## Requisitos

| Herramienta | Versión mínima |
|---|---|
| Docker Desktop | 4.x |
| Docker Compose | v2 (incluido en Docker Desktop) |

> No se necesita .NET, Node.js ni ningún otro runtime instalado localmente. Todo corre dentro de Docker.

## Levantar el stack

### macOS (Apple Silicon y Intel) y Windows

```bash
git clone https://github.com/LukeHowland/esports-platform.git
cd esports-platform
docker compose up --build
```

La primera vez tarda ~3–5 minutos (descarga imágenes y compila). Las siguientes levanta en ~60 segundos.

Esperar hasta ver en los logs:
```
esports-gateway  | Now listening on: http://0.0.0.0:8080
esports-seeder   | === Seeder completado con exito ===
```

El seeder corre automaticamente en cada arranque limpio y carga datos realistas conectados para todos los servicios.

## URLs

| Servicio | URL |
|---|---|
| **API Gateway** (frontend) | http://localhost:8080 |
| Swagger — teams | http://localhost:5001/swagger |
| Swagger — tournaments | http://localhost:5002/swagger |
| Swagger — matches | http://localhost:5003/swagger |
| Swagger — ranking | http://localhost:5004/swagger |
| Swagger — auth | http://localhost:5005/swagger |
| RabbitMQ Management | http://localhost:15672 (guest/guest) |

## Autenticación demo

Las lecturas Q1–Q24 son públicas. Las mutaciones requieren `Authorization: Bearer <token>` emitido por `auth`.

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"admin-dev-password"}'
```

Usuarios sembrados automáticamente:

| Rol | Usuario | Password | Uso |
|---|---|---|---|
| admin | `admin` | `admin-dev-password` | seeder/tests, puede todo |
| organizador | `org_riot`, `org_esl`, `org_blast`, etc. | `OrgDemo2024` | solo su `organizadorId` |
| capitan | `cap_t1`, `cap_navi`, `cap_g2`, etc. | `CapDemo2024` | solo su `equipoId` |
| fan | `fan_demo` | `FanDemo2024` | solo lectura |

## Endpoints principales (Q1–Q24)

```
GET  /api/jugadores/por-nickname/{nickname}          Q1
GET  /api/jugadores/por-pais/{pais}                  Q2
GET  /api/equipos/{id}/jugadores?pais=               Q3
GET  /api/equipos/por-fecha                          Q4
GET  /api/equipos/por-tag/{tag}                      Q5
GET  /api/equipos/{id}/integrantes                   Q6
GET  /api/ranking/equipos?top=10                     Q7
GET  /api/videojuegos/por-genero/{genero}            Q8
GET  /api/videojuegos/{id}/torneos                   Q9
GET  /api/organizadores                              Q10
GET  /api/organizadores/{id}/torneos                 Q11
GET  /api/torneos/por-fecha                          Q12
GET  /api/torneos/{id}/equipos                       Q13
GET  /api/torneos/por-equipo/{equipoId}              Q14
GET  /api/torneos/por-codigo/{codigo}                Q15
GET  /api/partidas/en-vivo/destacada                 Showcase live
GET  /api/partidas/por-torneo/{torneoId}             Q16
GET  /api/partidas/por-equipo/{equipoId}             Q17
GET  /api/partidas/por-fecha/YYYY-MM-DD              Q18
GET  /api/partidas/entre/{equipoId}/{rivalId}        Q19
GET  /api/torneos/{id}/premios                       Q20
GET  /api/premios/por-equipo/{equipoId}              Q21
GET  /api/ranking/victorias?top=10                   Q22
GET  /api/ranking/jugadores?top=10                   Q23
GET  /api/stats/equipo/{equipoId}/torneo/{torneoId}  Q24
```

## Tests de integración

El proyecto incluye 143 tests de integración que verifican Q1–Q24, casos de error, autorización por rol/ownership y el showcase live a través del gateway.

**Comando recomendado:** usar el runner limpio. Este comando baja el stack, corre tests contra una base recien sembrada y al terminar vuelve a levantar el stack demo limpio para que el frontend muestre solo datos del seeder.

```bash
./scripts/test-clean.sh
```

> Evitar `docker compose run --rm tests` sobre el stack de trabajo si despues vas a revisar el frontend manualmente. La suite crea datos temporales de QA; como Cassandra esta desnormalizada y ranking consume eventos, esos datos no se limpian fila por fila. El runner limpio resuelve esto con un reset completo antes y despues.

Los tests cubren:
- Q1–Q24: todos los endpoints de lectura con resultados esperados exactos
- Casos de error: 404, 400, cuerpos inválidos, GUIDs inexistentes, métodos HTTP incorrectos
- Auth: login, `/api/auth/me`, lecturas públicas, `401` sin token y `403` por rol/ownership incorrecto
- Invariantes: orden descendente en rankings, bidireccionalidad en Q19, suma `victorias + derrotas = partidas_jugadas`
- Showcase live: T1 vs Gen.G arranca 0-0, dura 30 minutos y al minuto 5 incluye dragon de T1

## Comandos útiles

```bash
# Ver logs de un servicio
docker compose logs -f tournaments

# Entrar a la consola CQL de Cassandra
docker compose exec cassandra cqlsh

# Bajar el stack. Al volver a levantar, Cassandra inicia limpia y el seeder repuebla.
docker compose down

# Arranque limpio con datos demo frescos
docker compose down
docker compose up --build
```

## Servicios

| Servicio | Keyspace | Puerto | Queries |
|---|---|---|---|
| teams | esports_teams | 5001 | Q1–Q6 |
| tournaments | esports_tournaments | 5002 | Q8–Q15, Q20, Q21 |
| matches | esports_matches | 5003 | Q16–Q19 |
| ranking | esports_ranking | 5004 | Q7, Q22–Q24 |
| auth | esports_auth | 5005 | login/JWT/roles |
| gateway | — | 8080 | proxy único |

## Troubleshooting

**El stack no levanta en Windows:** verificar que Docker Desktop use Linux containers (no Windows containers). Menú Docker > Switch to Linux Containers.

**Un servicio se reinicia en loop:** Cassandra puede tardar ~90 segundos en estar lista. Los servicios tienen retry automático con Polly. Esperar y revisar `docker compose logs cassandra`.

**Puerto 8080 ocupado:** cambiar el mapping en `docker-compose.yml` de `"8080:8080"` a `"8090:8080"`.
