# Project constitution

Principles that apply to **every** feature. A spec cannot contradict them; if it
must, change this file first, explicitly.

## 1. Required stack (challenge)

| Layer | Technology |
|-------|-----------|
| Backend | C# / .NET 8, ASP.NET Core, Entity Framework Core, PostgreSQL |
| Messaging | Azure Service Bus (official emulator locally; a real namespace via connection string) |
| Frontend | React + TailwindCSS (Vite) |
| Infrastructure | Docker / Docker Compose |
| AI (optional) | Claude through the official `Anthropic` C# SDK |

## 2. Non-negotiable business rules

1. An order has `id`, `customer`, `product`, `amount`, `status`, `createdAt`.
2. Statuses: `Pending`, `Processing`, `Completed` — **only** in that sequence.
3. Creating an order = persisting it to PostgreSQL through EF **and** publishing to Service Bus.
4. Messages carry `CorrelationId = OrderId` and `EventType = OrderCreated`.
5. The worker is **idempotent**: reprocessing the same message never causes an invalid transition or duplicated history.
6. Health checks cover the API, the database and the queue.

> The challenge brief names these in Portuguese (`cliente`, `produto`, `valor`,
> `data_criacao`; `Pendente`, `Processando`, `Finalizado`). They were renamed to
> English on purpose to present the project to an international audience
> ([spec 15](15-english-adaptation.md)); the rules themselves are unchanged.

## 3. Repository layout

```
api/Orders.Api/          REST API, outbox publisher, SignalR, AI module
worker/Orders.Worker/    Service Bus consumer
shared/Orders.Core/      Domain/ (aggregate, value objects, events), Application/ (use cases),
                         Infrastructure/ (DbContext, migrations, outbox), Contracts/ (messages)
tests/Orders.Tests/      unit, golden, integration (Testcontainers)
frontend/                React + Tailwind
infra/                   Service Bus emulator and pgAdmin configuration
specs/                   specifications (SDD)
docs/                    challenge brief
```

## 4. Architecture: tactical DDD, no strategic ceremony

The domain is small (one aggregate, three statuses, one core rule), so we apply
**only the tactical DDD patterns that solve real problems**:

| We use | For |
|--------|-----|
| **Rich aggregate** (`Order`) | Invariants protected by the model itself: private setters, `Order.Create` factory, status changes only through `TransitionTo` |
| **Value object** (`Money`) | An amount is always positive and has 2 decimal places, guaranteed by the domain and not only by the DTO |
| **Domain events** | The aggregate records what happened (`OrderCreated`, `OrderStatusChanged`); infrastructure turns them into outbox messages on `SaveChanges` |
| **Thin application layer** | Use cases (create, query, process) live outside the controllers and the worker host |
| **Domain exceptions** | A rule violation is a domain error, mapped to HTTP at the edge |
| **Ubiquitous language** | Business terms from the challenge (`Customer`, `Product`, `Amount`, `Pending`…) |

We deliberately **do not** use:

- **Generic repositories over EF** — the `DbContext` already is the unit of work and repository.
- **MediatR / formal CQRS** — three use cases do not justify the pipeline.
- **One project per layer** — `Orders.Core` separates `Domain/` (no EF or Service Bus dependency) and `Infrastructure/` by folder; the dependency rule is enforced by a test (spec 12).
- **Bounded contexts / context map** — there is a single context.

If the domain grows (payments, inventory, customers), revisit these decisions here.

## 5. Code conventions

- **The domain protects the rules**: status changes only through `Order.TransitionTo`; never `order.Status = ...` outside the domain.
- **Domain without infrastructure**: nothing in `Orders.Core/Domain` references EF Core, Npgsql or the Azure SDK.
- **Explicit contracts**: the API never exposes or accepts EF entities directly — it uses DTOs.
- **camelCase JSON**, enums as strings.
- **Configuration per environment**: secrets and addresses come from environment variables (`.env`); nothing sensitive is versioned.
- **No dead code**: replaced code is removed, not commented out.
- **English everywhere**: code, domain, API, UI, logs, comments, docs and commits.
- Structured logs (`ILogger` with message templates), always including `OrderId` when there is one.

## 6. Commit conventions

- Conventional Commits in English: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`, `ci:`.
- One commit per coherent feature/layer, referencing the spec (e.g. `feat(worker): idempotent consumer [spec 04]`).
- Nothing goes to `main` without review; work on a branch and open a PR.

## 7. Definition of done

A feature is **Done** when:

1. Every task in the spec is `[x]`.
2. Every acceptance criterion was verified (automated test or a manual step described in the spec).
3. `dotnet build` has no errors and `dotnet test` is green; in the frontend, `npm run lint` and `npm run build` are clean.
4. The spec and the table in [`README.md`](README.md) are up to date.
5. If running or configuring the project changed, the root `README.md` and `.env.example` were updated.
