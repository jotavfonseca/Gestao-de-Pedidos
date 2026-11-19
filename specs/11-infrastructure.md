# 11 — Infrastructure (Docker Compose)

- **Status:** Done
- **Score:** required (Documentation & DX 15%)

## Context

Compose did not start the worker or Service Bus, used the obsolete `version:` key,
there was no `.env.example`, and the API and worker Dockerfiles could not see shared code.

## Requirements

- Compose with API, Worker, Frontend, PostgreSQL and PgAdmin — challenge §3.
- `.env` for sensitive variables and a sample `.env` — challenge §3 and *Delivery*.
- Automatic migrations — challenge §3 (spec 02).
- Healthchecks in Compose — challenge §3 (spec 05).

## Acceptance criteria

1. **Given** a fresh clone, **when** `cp .env.example .env && docker compose up --build -d`, **then** within a few minutes every service is `healthy` and the full flow works without an Azure account.
2. Services: `postgres`, `pgadmin`, `sqlserver`, `servicebus`, `api`, `worker`, `frontend`, `jaeger`.
3. Startup order guaranteed by `depends_on: condition: service_healthy` (api after postgres and servicebus; worker after api; frontend after api).
4. pgAdmin opens with the PostgreSQL server already registered.
5. All passwords and keys come from `.env`; `.env` is git-ignored and `.env.example` is versioned.
6. Replacing `SERVICEBUS_CONNECTION_STRING` with a real Azure namespace works without code changes (the queues must exist in the namespace).
7. `docker compose down -v` removes everything, including data.

## Design

| Service | Image | Host port | Healthcheck |
|---------|--------|-----------|-------------|
| postgres | `postgres:16-alpine` | 5432 | `pg_isready` |
| pgadmin | `dpage/pgadmin4:8` | 8080 | — |
| sqlserver | `mcr.microsoft.com/mssql/server:2022-latest` | — | `sqlcmd` |
| servicebus | `mcr.microsoft.com/azure-messaging/servicebus-emulator:2.0.1` | 5672 | through the api `/health/ready` |
| api | build `api/Orders.Api/Dockerfile` (root context) | 5000 → 8080 | `curl /health/ready` |
| worker | build `worker/Orders.Worker/Dockerfile` (root context) | — | heartbeat |
| frontend | build `frontend/Dockerfile` | 3000 → 80 | `wget` |
| jaeger | `jaegertracing/all-in-one:1.62.0` | 16686 | — |

- `infra/servicebus/config.json`: namespace `sbemulatorns`, queues `order-created` and `order-status-changed` (`MaxDeliveryCount: 5`, `LockDuration: PT30S`, dead-letter on expiration).
- `infra/pgadmin/servers.json` mounted at `/pgadmin4/servers.json`.
- `.dockerignore` at the root (bin, obj, node_modules, .git, .env).
- Variables (`.env.example`): `POSTGRES_*`, `PGADMIN_*`, `MSSQL_SA_PASSWORD`, `SERVICEBUS_CONNECTION_STRING`, `ANTHROPIC_API_KEY` (optional), `ASPNETCORE_ENVIRONMENT`, `WORKER_PROCESSING_DELAY_SECONDS`, ports.
- `.gitignore`: `.env` and `.env.*` ignored, with an `!.env.example` exception.

## Tasks

- [x] 11.1 Rewrite `docker-compose.yml` with the 8 services and healthchecks.
- [x] 11.2 `infra/servicebus/config.json` with the queues.
- [x] 11.3 `infra/pgadmin/servers.json`.
- [x] 11.4 `.env.example` and the `.gitignore` fix.
- [x] 11.5 Root `.dockerignore`; API and worker Dockerfiles with the root context and restore in its own layer.

## Verification

- Fresh clone → `cp .env.example .env && docker compose up --build -d` → `docker compose ps` all `healthy`.
- Spec 04 flow end to end.
- `http://localhost:8080` (pgAdmin) with the server already listed.

## Implementation notes

- The PostgreSQL volume was renamed to `orders_pgdata`: the earlier `gestao-de-pedidos_postgres_data` volume (PostgreSQL 15 + old schema) is incompatible and was left untouched, unused.
- The Compose project name stays `gestao-de-pedidos` (the repository name), which keeps the existing data volume.
- Verified: `docker compose config`, build of the 3 images and a full start with the end-to-end flow working.
