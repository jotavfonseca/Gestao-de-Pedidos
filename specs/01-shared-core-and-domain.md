# 01 — Shared core and domain (tactical DDD)

- **Status:** Done
- **Score:** foundation (enables 02, 03, 04, 07); Code Quality 30%

## Context

The domain and the `DbContext` lived inside the API web project, and the worker
referenced that whole project. The worker used `_context.Historico`, which did not
exist, so it did not compile. Any code could change `Status` freely, so the mandatory
sequence was not enforced anywhere. We follow the tactical DDD defined in
[`constitution.md` §4](constitution.md#4-architecture-tactical-ddd-no-strategic-ceremony).

## Requirements

- Order attributes and statuses — challenge §1 *Business rules*.
- Mandatory sequence Pending → Processing → Completed — challenge §1 *Additional rules*.
- EF + PostgreSQL — challenge *Required technologies*.

## Acceptance criteria

Aggregate:
1. **Given** a new order, **when** `Order.Create`, **then** it is `Pending`, its history has 1 `Pending` entry and there is 1 `OrderCreated` domain event.
2. **Given** a `Pending` order, **when** `TransitionTo(Processing)`, **then** the status changes, the history gains 1 entry and 1 `OrderStatusChanged` event is recorded.
3. **Given** a `Pending` order, **when** `TransitionTo(Completed)`, **then** `InvalidStatusTransitionException` is thrown and nothing changes (no history, no events).
4. **Given** a `Completed` order, **when** any `TransitionTo`, **then** the exception is thrown.
5. **Given** an empty `customer` or `product`, or longer than 100 characters, **when** `Order.Create`, **then** `DomainValidationException` is thrown with the field name.

Value object:
6. **Given** a value `≤ 0`, **when** `Money.Of(value)`, **then** `DomainValidationException`.
7. **Given** `Money.Of(10.005m)`, **then** the value is rounded to 2 places (`10.01`, `MidpointRounding.AwayFromZero`).
8. Two `Money` instances with the same value are equal (value equality).

Persistence and events:
9. **Given** a migrated database, **then** `Orders`, `OrderStatusHistory` and `OutboxMessages` exist, with `Status` as text and `Amount` as `numeric(18,2)`.
10. **Given** an aggregate with pending events, **when** `SaveChangesAsync`, **then** each event becomes an `OutboxMessage` **in the same transaction** and the aggregate's events are cleared.
11. **Given** two concurrent updates of the same order, **when** both save, **then** the second gets `DbUpdateConcurrencyException`.

Architecture:
12. Nothing in `Orders.Core.Domain` references EF Core, Npgsql or the Azure SDK (enforced by a test — spec 12).
13. Worker and API reference only `Orders.Core` (the worker does not reference the API).

## Design

New project `shared/Orders.Core` (net8.0 class library), split by folder:

```
Domain/                               # no infrastructure dependencies
  Abstractions/
    AggregateRoot.cs                  # list of IDomainEvent (Raise, ClearDomainEvents)
    IDomainEvent.cs                   # OccurredAt
    DomainException.cs                # base class
  Orders/
    Order.cs                          # Create(customer, product, Money, now); TransitionTo(status, now)
    OrderStatus.cs                    # Pending, Processing, Completed
    OrderStatusHistory.cs             # child entity of the aggregate
    Money.cs                          # value object (record), Of(decimal)
    Events/OrderCreated.cs            # OrderId, Customer, Product, Amount, OccurredAt
    Events/OrderStatusChanged.cs      # OrderId, From, To, OccurredAt
    InvalidStatusTransitionException.cs
    DomainValidationException.cs
Application/                          # use cases (specs 02 and 04)
Contracts/                            # messages sent over Service Bus
  Messages.cs                         # OrderCreatedMessage, OrderStatusChangedMessage, MessageSerializer
  MessagingConstants.cs               # queues and EventType
Infrastructure/
  Persistence/
    OrderContext.cs
    Configurations/OrderConfiguration.cs, OutboxMessageConfiguration.cs
    DesignTimeOrderContextFactory.cs
    Migrations/
  Outbox/
    OutboxMessage.cs                  # Id, Type, Destination, Payload, CorrelationId, TraceParent, CreatedAt, ProcessedAt, Attempts, LastError
    DomainEventsToOutboxInterceptor.cs  # SaveChangesInterceptor
    DomainEventMapper.cs              # domain event → (queue, EventType, contract)
Observability/
  Telemetry.cs                        # ActivitySource "Orders.Messaging" (spec 08)
```

Decisions:
- **`Order` aggregate**: private setters; private constructor for EF; `StatusHistory` exposed as `IReadOnlyCollection`. Status changes only through `TransitionTo`, which validates the sequence, records history and raises the event.
- **`Money`** mapped as an EF **complex property** to the `Amount` column (`numeric(18,2)`); in the API JSON it is still a number (`amount`).
- **Domain events × messages**: domain events are internal (`Domain/Orders/Events`); what goes to the queue are stable **contracts** (`Contracts/`). `DomainEventMapper` translates between them, so the domain can evolve without breaking consumers.
- **Interceptor** (`SaveChangesInterceptor`): before saving, collects events from tracked aggregates, creates the `OutboxMessage` rows and clears the events. Same transaction, no messaging code in the use cases.
- **Injectable clock**: methods take a `DateTimeOffset now`; use cases use `TimeProvider` (deterministic tests).
- **Optimistic concurrency** with PostgreSQL's `xmin` column.
- Status stored as `string` (`HasConversion<string>()`).
- The API's old migrations were **discarded** and `InitialCreate` regenerated (disposable database at that stage).

## Tasks

- [x] 1.1 Create `Orders.sln` at the root with API, worker, core and tests.
- [x] 1.2 Create `shared/Orders.Core` with `Domain/`, `Application/`, `Contracts/`, `Infrastructure/`; remove `Models/`, `Data/` and `Migrations/` from the API.
- [x] 1.3 `AggregateRoot`, `IDomainEvent`, domain exceptions.
- [x] 1.4 `Money` value object.
- [x] 1.5 `Order` aggregate with `Create`, `TransitionTo`, history and the `OrderCreated` / `OrderStatusChanged` events.
- [x] 1.6 `OrderContext` + Fluent configurations; status as text; `Money` mapped; `xmin` as concurrency token.
- [x] 1.7 `OutboxMessage` (with `Destination`) and an index on `(ProcessedAt, CreatedAt)`.
- [x] 1.8 Message contracts and `MessagingConstants`.
- [x] 1.9 `DomainEventMapper` and `DomainEventsToOutboxInterceptor`.
- [x] 1.10 Design-time factory and regenerated `InitialCreate` migration.

## Verification

- `dotnet build Orders.sln` with no errors.
- Criteria 1–8: domain unit tests (spec 12).
- Criteria 9–11: integration tests with Testcontainers (spec 12).
- Criterion 12: reflection-based architecture test (spec 12).
- Criterion 13: `worker/Orders.Worker/Orders.Worker.csproj` only references `Orders.Core`.

## Implementation notes

- `Money` is an EF 8 **complex property**, not a value conversion, so `o.Amount.Value` translates to SQL (the AI module's sums run in the database).
- `Money.Value` (not `Amount`) avoids the confusing `order.Amount.Amount`.
- Versions: `Microsoft.Extensions.*` 10.0 (they support `net8.0`) to use OpenTelemetry 1.15.3, which fixes GHSA-4625-4j76-fww9. Versions are centralized in `Directory.Packages.props`; `dotnet-ef` 8.0.11 is pinned in `.config/dotnet-tools.json`.
- Verified: criteria 1–8 by `OrderTests`/`MoneyTests`; 9–11 by `PersistenceTests` (Testcontainers); 12 by `LayeringTests`.
