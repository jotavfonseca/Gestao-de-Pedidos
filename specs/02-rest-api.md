# 02 — REST API

- **Status:** Done
- **Score:** required (Functionality 15%, Code Quality 30%)

## Context

The three endpoints existed, but POST accepted the whole EF entity (clients could
send `status` or the history), the enum was serialized as a number, errors became a
generic `Exception`, and migrations ran without retry (the API crashed if the
database was slow to start).

## Requirements

- Endpoints `POST /orders`, `GET /orders`, `GET /orders/{id}` — challenge §1.
- Persist with EF to PostgreSQL — challenge §1 *Business rules*.
- Automatic migrations — challenge §3.

## Acceptance criteria

1. **Given** `{ "customer": "Jane Doe", "product": "Keyboard", "amount": 199.9 }`, **when** `POST /orders`, **then** `201 Created`, a `Location: /orders/{id}` header and a body with `status: "Pending"`.
2. **Given** a body with `status: "Completed"`, **when** `POST /orders`, **then** the field is ignored and the order starts as `Pending`.
3. **Given** an empty `customer`, a `product` longer than 100 characters or `amount <= 0`, **when** `POST /orders`, **then** `400` with `ValidationProblemDetails` naming the field.
4. **Given** existing orders, **when** `GET /orders`, **then** `200` with a list ordered by `createdAt` descending.
5. **Given** an existing id, **when** `GET /orders/{id}`, **then** `200` with `statusHistory` ordered by date.
6. **Given** an unknown id (or a non-GUID), **when** `GET /orders/{id}`, **then** `404` with `ProblemDetails`.
7. **Given** an unexpected error, **then** `500` with `ProblemDetails`, without a stack trace outside Development.
7a. **Given** a domain rule violated after DTO validation (e.g. `DomainValidationException`), **then** `400` with `ProblemDetails`; `InvalidStatusTransitionException` becomes `409 Conflict`.
7b. The controller contains no business rules and no direct `DbContext` access: it only translates HTTP ↔ use case.
8. **Given** PostgreSQL still starting, **when** the API starts, **then** it retries the migrations and only accepts requests after applying them.

## Design

```
shared/Orders.Core/Application/Orders/
  CreateOrder.cs                    # CreateOrderCommand + CreateOrderHandler: Money.Of, Order.Create, SaveChanges
  OrderQueries.cs                   # GetById, List (AsNoTracking)
  OrderDto.cs                       # read model used by the API and the tests
  ApplicationServiceCollectionExtensions.cs  # AddOrdersApplication()

api/Orders.Api/
  Contracts/CreateOrderRequest.cs   # DataAnnotations: Required, StringLength(100), Range(0.01, ...)
  Contracts/OrderResponse.cs        # id, customer, product, amount, status, createdAt, statusHistory[]
  Controllers/OrdersController.cs   # HTTP ↔ use case only
  Infrastructure/DomainExceptionHandler.cs  # IExceptionHandler: domain exceptions → 400/409
  Infrastructure/DatabaseMigrator.cs
  Program.cs
  Orders.Api.http
```

Thin application layer (tactical DDD, no MediatR): plain classes registered in DI
and injected into the controller. Validation happens in two layers with different
roles: the DTO gives friendly per-field feedback (400 with `ValidationProblemDetails`);
the domain guarantees the invariant regardless of the caller.

Response shape:

```json
{
  "id": "4f0c…",
  "customer": "Jane Doe",
  "product": "Keyboard",
  "amount": 199.90,
  "status": "Pending",
  "createdAt": "2026-11-19T13:00:00+00:00",
  "statusHistory": [{ "status": "Pending", "changedAt": "2026-11-19T13:00:00+00:00" }]
}
```

Decisions:
- `GET /orders` also returns `statusHistory` (small list; avoids N+1 in the frontend). Pagination is out of scope.
- Global `JsonStringEnumConverter`.
- `AddProblemDetails()` + `UseExceptionHandler()`; the controller does not catch generic exceptions.
- Migration retry: up to 10 attempts, growing delay, a log line per failure.

## Tasks

- [x] 2.1 `CreateOrderRequest` and `OrderResponse` DTOs (+ mapping from `OrderDto`).
- [x] 2.2 `CreateOrderHandler` and `OrderQueries` use cases in `Orders.Core/Application`.
- [x] 2.3 Rewrite `OrdersController` (file renamed) delegating to the use cases.
- [x] 2.4 `JsonStringEnumConverter`, `ProblemDetails`, `DomainExceptionHandler`, removal of `throw new Exception`.
- [x] 2.5 `DatabaseMigrator` with retry, called before `app.Run()`.
- [x] 2.6 `Orders.Api.http` with real examples (create, list, details, invalid, 404).

## Verification

- Criteria 1–7a: API integration tests (spec 12, `WebApplicationFactory` + Testcontainers).
- Criterion 8: start with `docker compose up` and check the API log for the retries until PostgreSQL is ready.

## Implementation notes

- **CORS removed**: the browser always talks to the same origin (nginx proxy in Docker, Vite proxy in dev), so CORS is unnecessary.
- `[Produces("application/json")]` was removed from the controllers: it forced `application/json` on errors too, which must be `application/problem+json`.
- `[Range]` uses `ParseLimitsInInvariantCulture`: on pt-BR machines the `"0.01"` limit could not be parsed.
- Verified: criteria 1–7 by `OrdersApiTests` and manually with `curl`; criterion 8 in the Compose logs.
