# 04 — Idempotent worker

- **Status:** Done
- **Score:** required (Messaging & Reliability 20%)

## Context

The previous worker did not compile, injected a scoped `DbContext` into a singleton
(startup failure), polled the database instead of consuming Service Bus and was not
in Compose. There was no real idempotency: two instances would process the same order.

## Requirements

- The worker consumes messages, moves the order to `Processing` and, after 5 s, to `Completed` — challenge §1.
- Idempotent consumer — challenge §1 *Additional rules*.
- Mandatory status sequence — challenge §1.

## Acceptance criteria

1. **Given** an `OrderCreated` message for a `Pending` order, **when** the worker processes it, **then** the order becomes `Processing` immediately and `Completed` ~5 s later, with 2 new history entries; the message is completed.
2. **Given** the same message delivered again after the order is `Completed`, **then** nothing changes in the database and the message is completed.
3. **Given** a redelivery while the order is `Processing` (the worker crashed midway), **then** the worker resumes and completes it, without duplicating the `Processing` history entry.
4. **Given** two simultaneous deliveries of the same message, **then** only one performs each transition (the other gets a concurrency conflict, reloads and behaves like 2 or 3).
5. **Given** a message whose order does not exist or whose body is invalid, **then** it goes to the dead-letter queue with a descriptive reason.
6. **Given** a message with an `EventType` other than `OrderCreated`, **then** it goes to the dead-letter queue.
7. **Given** a transient error (e.g. database unavailable), **then** the message is abandoned and redelivered up to `MaxDeliveryCount`, then dead-lettered.
8. On every transition, the `OrderStatusChanged` domain event becomes an outbox message in the same transaction and is published to the `order-status-changed` queue (used by spec 06) — no direct Service Bus send inside the handler.

## Design

```
shared/Orders.Core/Application/Orders/
  ProcessOrder.cs               # idempotent use case: loads the aggregate and applies TransitionTo based on its state

worker/Orders.Worker/
  Program.cs                    # Host + DI + ServiceBusProcessor + outbox publisher
  OrderCreatedConsumer.cs       # BackgroundService: starts/stops the ServiceBusProcessor
  OrderCreatedHandler.cs        # adapter: validates the message, calls ProcessOrder, decides complete/dead-letter/abandon
  Heartbeat.cs                  # writes the heartbeat file every 10 s
```

`ProcessOrder` (application) knows nothing about Service Bus: it receives the
`OrderId`, uses the aggregate and saves. `OrderCreatedHandler` (worker) only
translates the message into the use case and the outcome into a queue action.

- `ServiceBusProcessor` with `AutoCompleteMessages = false`, configurable `MaxConcurrentCalls` (default 4), default lock (30 s > 5 s of processing).
- **One DI scope per message** (`IServiceScopeFactory`) → a new `OrderContext` per message.
- Idempotency = **state machine + optimistic concurrency**: the order's current state decides what to do; `xmin` prevents two deliveries from making the same transition. No "processed messages" table is needed because each transition can only happen once.
- Handler outcome: `Complete`, `DeadLetter(reason)` or an exception (→ abandon).
- 5 s delay configurable (`Processing__DelaySeconds`) so tests run fast.
- `OrderStatusChanged` goes out through the **outbox** (spec 03): the notification is written in the same transaction as the status change, so it is not lost if Service Bus fails at that moment.
- The project only references `Orders.Core`; Dockerfile with the repository root as build context.

## Tasks

- [x] 4.1 Replace polling with a `ServiceBusProcessor` on the `order-created` queue.
- [x] 4.2 `ProcessOrder` use case in `Orders.Core/Application` (criteria 1–4), handling `DbUpdateConcurrencyException`.
- [x] 4.3 `OrderCreatedHandler` with one scope per message, calling `ProcessOrder`.
- [x] 4.4 `EventType`/body validation and dead-lettering (criteria 5–7).
- [x] 4.5 Register the outbox publisher in the worker (criterion 8).
- [x] 4.6 Heartbeat for the healthcheck.
- [x] 4.7 Remove the API reference; Dockerfile with the repository root as context.

## Verification

- Criteria 1–4 and 8: `ProcessOrder` tests with PostgreSQL in Testcontainers (spec 12), also checking the generated `OutboxMessage`.
- Criteria 5–7: handler tests + dead-letter inspection in the log.
- End to end: Compose running, create an order and follow `docker compose logs -f worker`.

## Implementation notes

- Verified: criteria 1–4 and 8 by `ProcessOrderTests` (including two concurrent deliveries: exactly one completes); 5–6 by `OrderCreatedHandlerTests`; full flow in Compose.
- Criterion 7 (abandon up to `MaxDeliveryCount` = 5) depends on Service Bus behavior and has no automated test.
