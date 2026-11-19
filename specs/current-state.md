# Starting point (assessment of Nov 19, 2026)

A snapshot of the project **before** specs 01–13. It is the baseline: every problem
listed here is solved by at least one spec. Names are the original Portuguese ones
used by the code at the time.

## Summary

The project **did not run end to end**: the worker did not compile, the API did not
publish to Service Bus, the worker was not in Compose, and the details page did not exist.

## By area

### API — `api/Orders.Api`

| Existed | Problem | Solved in |
|--------|----------|--------------|
| `POST /orders`, `GET /orders`, `GET /orders/{id}` | POST accepted the whole EF entity (clients could send `status`, `historico`); caught exceptions and rethrew a generic `Exception` | 02 |
| `ServiceBusPublisher` | **Never called** (comment: "no publishing to the queue"); created a sender per send and never disposed it; used `Subject` instead of `EventType` | 03 |
| `OrderContext` + `InitialCreate` migration | `OrderStatusHistory` was in the migration but had no `DbSet` | 01 |
| `OrderStatus` enum | Serialized as a number; the frontend expected text | 02 |
| `/health` | Only checked PostgreSQL; nothing for the queue | 05 |
| Migrations on startup | No retry: if the database was slow, the API crashed | 02 |
| `Orders.Api.http` | Pointed to `/weatherforecast` (template leftover) | 02 |

### Worker — `worker/Orders.Worker`

| Existed | Problem | Solved in |
|--------|----------|--------------|
| `BackgroundService` doing Pendente → Processando → Finalizado with 5 s | **Did not compile**: used `_context.Historico`, which did not exist on `OrderContext` | 01, 04 |
| `OrderContext` injection | Scoped `DbContext` inside a singleton hosted service → startup error | 04 |
| Reading orders | **Polling the database**; did not use Service Bus; no real idempotency | 04 |
| `ProjectReference` to the API | Worker depended on the web project; a Dockerfile with only the worker as context could not see the API | 01, 04 |
| — | **Not in docker-compose** | 11 |

### Frontend — `frontend`

| Existed | Problem | Solved in |
|--------|----------|--------------|
| Orders list with a badge | No automatic status updates | 06, 09 |
| Create form | No validation beyond `required` | 09 |
| `OrderDetails.jsx` | **A copy of the create form**; showed no details | 07, 09 |
| Tailwind | Not wired: `index.css` was a static precompiled CSS; `@tailwindcss/vite` installed but unused; stray `input.css`/`postcss.config.mjs` | 09 |
| `services/api.js` | Hard-coded URL `http://localhost:5000` | 09 |
| Dockerfile | Node 18 (Vite 7 requires Node ≥ 20) | 09 |
| — | Vite template leftovers (`App.css`, `react.svg`, legacy `.eslintrc.json`, "Vite + React" title) | 09 |

### Infrastructure

| Existed | Problem | Solved in |
|--------|----------|--------------|
| Compose with postgres, pgadmin, api, frontend | No worker and no Service Bus; obsolete `version:`; frontend healthcheck relied on `wget` without guarantees | 11 |
| `.env` | No `.env.example`; `.gitignore` used `*.env` | 11 |

### Documentation and tests

| Existed | Problem | Solved in |
|--------|----------|--------------|
| `README.md` | Generic; referenced non-existent paths (`./src/Api/Api.csproj`) and unused variables | 13 |
| — | **No automated tests** | 12 |
| — | No architecture diagram | 13 |
