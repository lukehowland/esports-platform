# 08 — Commits

> Reglas de commit para el equipo y los agentes. El objetivo: un historial legible donde cada commit cuenta una sola cosa, y se puede revertir o revisar sin arrastrar cambios no relacionados.

## Regla de oro: commits atómicos

Cada commit es **un cambio lógico, completo y autocontenido**:

- No mezclar features con formateo, refactors o fixes no relacionados en el mismo commit.
- Si tocaste dos cosas distintas (ej. agregaste un endpoint Y arreglaste un bug en otro servicio), son **dos commits**.
- Un commit debe poder revertirse solo (`git revert`) sin romper otra cosa.
- Idealmente, el repo compila/levanta en cada commit (no dejar commits a medio terminar tipo "WIP").
- Tamaño: preferí varios commits chicos y enfocados sobre uno gigante que toca 15 archivos por razones distintas.

## Idioma: commits en inglés

A diferencia del dominio de negocio (que va en español, ver `docs/03-convenciones.md`), **los mensajes de commit van siempre en inglés**, tipo y descripción incluidos. Es el estándar de la industria y lo que esperan herramientas/integraciones (GitHub, changelogs, etc.).

## Formato: Conventional Commits

```
<type>(<scope>): <short imperative description>

[optional body]

[optional footer]
```

- **type**: ver tabla abajo.
- **scope** (opcional pero recomendado): el servicio o área afectada, en minúscula — `teams`, `tournaments`, `matches`, `ranking`, `gateway`, `shared`, `docs`, `infra`.
- **description**: imperativo, minúscula, sin punto final. ("add", no "added"/"adds").
- **body** (opcional): el *por qué*, no el *qué* (el diff ya muestra el qué). Útil para decisiones no obvias.
- **footer** (opcional): `BREAKING CHANGE: ...` si rompe un contrato, o referencias a issues.

### Tipos

| Type | Cuándo usarlo |
|---|---|
| `feat` | Nueva funcionalidad visible (endpoint, evento, campo) |
| `fix` | Corrección de un bug |
| `docs` | Solo documentación (`docs/`, `README`, comentarios) |
| `chore` | Tareas de mantenimiento sin afectar src/test (deps, config, `.gitignore`) |
| `refactor` | Cambio interno sin alterar comportamiento observable |
| `test` | Agregar o corregir tests |
| `style` | Formato/estilo (espacios, linting) sin cambio de lógica |
| `perf` | Mejora de performance |
| `build` | Cambios en build, Dockerfiles, `.csproj`, `docker-compose.yml` |
| `ci` | Pipelines de CI/CD |
| `revert` | Revertir un commit previo |

## Ejemplos (buenos)

```
feat(teams): add players by country endpoint (Q3)
feat(tournaments): publish TeamRegisteredToTournament on enrollment
fix(ranking): increment total_torneos via counter instead of read-then-write
docs: add API contracts for matches service
docs: add commit conventions
chore: add .gitattributes and .gitignore
chore(shared): pin MassTransit.RabbitMQ to 8.*
refactor(tournaments): extract team lookup into typed HttpClient
build(teams): add multi-stage Dockerfile
test(matches): add integration test for Q8 history endpoint
```

## Ejemplos (a evitar)

```
update stuff                          # no dice qué ni por qué
fix bug                                # type sin contexto, descripción vacía
WIP                                    # no es un commit terminado
feat: add teams, tournaments and fix ranking bug   # mezcla varios cambios → separar en commits
Fix Ranking Counter.                   # mayúsculas y punto final innecesarios
```

## Cómo dividir un cambio grande

Si una feature toca varios servicios (ej. Fase 2 agrega `tournaments` completo), está bien que sean **varios commits secuenciales dentro de la misma fase/rama**, por ejemplo:

```
feat(tournaments): scaffold service structure and Cassandra schema
feat(tournaments): add tournament and prize endpoints (Q5, Q6, Q7)
feat(tournaments): add team enrollment with REST lookup to teams (Q1, Q2)
feat(tournaments): publish TeamRegisteredToTournament on enrollment
build(tournaments): add Dockerfile and wire into docker-compose
```

Cada uno debe ser revisable de forma independiente y, salvo el primero de una secuencia de scaffolding, no debería romper el build.

## Relación con `docs/03-convenciones.md`

Esta doc reemplaza/precisa la sección "Git" de `docs/03-convenciones.md`: las ramas (`feat/teams`, `feat/tournaments`, etc.) y el flujo de merge siguen igual, pero el **formato y el idioma del mensaje de commit** son los definidos acá.
