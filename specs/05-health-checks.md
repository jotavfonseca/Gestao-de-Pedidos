# 05 — Health checks

- **Status:** Done
- **Score:** required

## Context

`/health` only checked PostgreSQL. There was no queue check, no split between
*liveness* and *readiness*, and the worker (not a web app) had no Compose healthcheck.

## Requirements

- Health checks for the API, the database and the queue — challenge §1 *Additional rules*.
- Healthchecks in Compose — challenge §3.

## Acceptance criteria

1. **When** `GET /health/live`, **then** `200` while the API process is up (does not depend on the database or the queue).
2. **When** `GET /health/ready` (or `/health`), **then** the JSON lists `self`, `postgres` and `servicebus` with status and duration; `200` if all are `Healthy`, `503` if any is `Unhealthy`.
3. **Given** PostgreSQL stopped, **then** `/health/ready` returns `503` with `postgres: Unhealthy`.
4. **Given** the Service Bus emulator stopped, **then** `/health/ready` returns `503` with `servicebus: Unhealthy`.
5. In Compose, `docker compose ps` shows `healthy` for every service with a healthcheck when everything is up.

## Design

- `api/Orders.Api/Health/ServiceBusHealthCheck.cs`: creates a receiver for the `order-created` queue and calls `PeekMessageAsync` with a 5 s timeout. Chosen because the emulator does **not** support the management operations used by `AspNetCore.HealthChecks.AzureServiceBus`.
- PostgreSQL: `AddNpgSql` (`AspNetCore.HealthChecks.NpgSql` package).
- A `live` tag separates the endpoints; custom JSON response (`status`, `totalDuration`, `checks[]`).
- Worker: heartbeat file updated every 10 s; the Compose healthcheck checks it was modified within the last minute.
- Frontend (nginx): `wget -q --spider` (alpine busybox).

## Tasks

- [x] 5.1 `/health/live`, `/health/ready` and `/health` endpoints with a JSON writer.
- [x] 5.2 `ServiceBusHealthCheck` with peek and timeout.
- [x] 5.3 Healthchecks for every service in Compose (done with spec 11).

## Verification

- `curl -i localhost:5000/health/live` and `curl -s localhost:5000/health/ready | jq`.
- Criteria 3–4: `docker compose stop postgres` / `stop servicebus` and repeat the curl.
- Criterion 5: `docker compose ps`.

## Implementation notes

- The API's Compose healthcheck **uses `/health/ready`**: the container only becomes `healthy` when PostgreSQL and Service Bus are ready, and the worker/frontend wait for it.
- The Service Bus emulator has no healthcheck of its own (the image has no tool for it); its readiness is measured by the API's `servicebus` check. Jaeger and pgAdmin have no healthcheck either.
- Frontend: `wget` on `127.0.0.1` (on `localhost` busybox may try IPv6).
- Verified: `docker compose ps` with api, worker, frontend, postgres and sqlserver `healthy`; `/health/ready` = 503 with Service Bus stopped.
