# 12 — Automated tests

- **Status:** Done
- **Score:** bonus **Testcontainers (+1)** and **Golden Tests (+2)**; Automated Tests 10%

## Context

There were no tests. The most critical rules (status sequence, idempotency,
transactional outbox) need automated proof.

## Requirements

- Testcontainers and Golden Tests — challenge §5.
- Automated tests — evaluation criteria.

## Acceptance criteria

1. `dotnet test` at the root runs every test.
2. **Domain unit tests**: valid and invalid transitions, history and domain events; the `Money` value object (validation, rounding, equality); `customer`/`product` validation in the aggregate.
2a. **Architecture**: a reflection test ensures no type in `Orders.Core.Domain` depends on EF Core, Npgsql or the Azure SDK, and `Application` does not depend on `Azure.Messaging.ServiceBus`.
2b. **Outbox**: with Testcontainers, saving an aggregate with events produces the right `OutboxMessage` rows (destination, `EventType`, `CorrelationId`, payload) in the same transaction.
3. **Golden tests**: the `OrderResponse` and message JSON is compared with versioned files in `tests/Orders.Tests/Golden/files/`; a contract change breaks the test with a readable diff; `UPDATE_GOLDEN=1 dotnet test` regenerates them.
4. **Worker with Testcontainers (PostgreSQL)**: (a) happy path, (b) duplicate message after completion, (c) redelivery while `Processing`, (d) concurrent deliveries, (e) unknown order → dead-letter.
5. **API with Testcontainers**: POST creates the order **and** the outbox row; validation 400; GET by id with history; 404; ordered list.
6. **AI module tools**: correct counts, sums and average time with known data.
7. **Health**: `/health/live` returns 200 on the test API.
8. Without Docker, integration tests are **skipped** with a clear notice; unit and golden tests still run.
9. Frontend: `npm run lint` and `npm run build` clean.

## Design

```
tests/Orders.Tests/
  Domain/OrderTests.cs, MoneyTests.cs
  Architecture/LayeringTests.cs            # reflection over type references (no extra package)
  Golden/GoldenFile.cs                     # Assert helper + UPDATE_GOLDEN
  Golden/ContractGoldenTests.cs, files/*.json
  Infrastructure/PostgresFixture.cs        # Testcontainers.PostgreSql, migrated once per run
  Infrastructure/DockerAvailability.cs     # [DockerFact]: skips when Docker is unavailable
  Infrastructure/PersistenceTests.cs       # outbox, rollback, text status, xmin
  Infrastructure/MigrationTests.cs         # RenameToEnglish upgrade on Portuguese data (spec 15)
  Application/ProcessOrderTests.cs         # worker use case idempotency
  Worker/OrderCreatedHandlerTests.cs       # message validation and dead-letter
  Api/ApiFactory.cs, OrdersApiTests.cs     # WebApplicationFactory; Service Bus disabled
  Ai/OrderAnalyticsToolsTests.cs
```

- xUnit, `Testcontainers.PostgreSql`, `Microsoft.AspNetCore.Mvc.Testing`.
- Golden files with a custom helper (normalized JSON comparison), no extra dependency.
- Fixed dates for deterministic JSON.
- Worker delay set to 0 s in the tests.
- `OutboxPublisher` and Service Bus consumers turned off in `ApiFactory`.

## Tasks

- [x] 12.1 `tests/Orders.Tests` project in the solution.
- [x] 12.2 Domain tests (aggregate, events, `Money`).
- [x] 12.2a Architecture test (layers).
- [x] 12.2b Outbox interceptor test.
- [x] 12.3 Golden tests + helper + files.
- [x] 12.4 `PostgresFixture` and `DockerAvailability`.
- [x] 12.5 `ProcessOrder` (idempotency) and `OrderCreatedHandler` (dead-letter) tests.
- [x] 12.6 `ApiFactory` and API tests.
- [x] 12.7 AI module tool tests.
- [x] 12.8 Frontend lint and build verified.

## Verification

- `dotnet test` with Docker on → all green.
- `dotnet test` with Docker off → integration skipped, everything else green.
- Change a field of `OrderResponse` → golden fails; `UPDATE_GOLDEN=1` regenerates.

## Implementation notes

- 53 tests, all passing (`dotnet test`, ~20 s with Docker), also in CI on Linux.
- The skip-without-Docker path was not exercised (Docker was always on).
- `WebApplicationFactory` uses `OrdersController` as the assembly marker, because the worker also exposes a `Program` class to the tests.
- Golden files are written without Unicode escapes for readable diffs.
