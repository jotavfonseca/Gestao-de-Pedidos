# 03 — Reliable messaging: Outbox + Service Bus

- **Status:** Done
- **Score:** required + bonus **Outbox Pattern (+3)** (Messaging & Reliability 20%)

## Context

`ServiceBusPublisher` existed but was never called — creating an order published
nothing. Publishing straight from the controller would have the classic *dual write*
problem: if the database commits and the send fails (or the other way around), order
and message diverge.

With tactical DDD (spec 01), the aggregate records **domain events** and an EF
interceptor turns them into `OutboxMessage` rows on `SaveChanges`. No use case builds
a message by hand. The same mechanism is used by the API (`OrderCreated`) and the
worker (`OrderStatusChanged`).

## Requirements

- On order creation, persist and publish to Azure Service Bus — challenge §1.
- `CorrelationId = OrderId` and `EventType = OrderCreated` — challenge §1 *Additional rules*.
- Outbox Pattern — challenge §5 (bonus).

## Acceptance criteria

1. **Given** a successful `POST /orders`, **then** the order and an `OutboxMessage` (`Type = OrderCreated`, `Destination = order-created`, `ProcessedAt = null`) were written **in the same transaction**, without the use case referencing the outbox.
2. **Given** a failed save, **then** neither the order nor the message exist.
3. **Given** a pending `OutboxMessage` and Service Bus available, **when** the publisher runs, **then** within ~2 s the message is on the target queue and `ProcessedAt` is set.
4. The published `OrderCreated` message has: `MessageId = OutboxMessage.Id`, `CorrelationId = OrderId`, `Subject = "OrderCreated"`, `ApplicationProperties["EventType"] = "OrderCreated"`, `ContentType = application/json` and an `OrderCreatedMessage` body.
5. **Given** Service Bus down, **when** the publisher tries to send, **then** the message stays pending, `Attempts` and `LastError` are updated and it is resent once Service Bus is back; the API keeps accepting orders.
6. **Given** two publisher instances, **then** the same message is not sent twice in the normal flow (and if it is, the worker's idempotency — spec 04 — absorbs it).
7. **Given** a status transition made by the worker, **then** an `OrderStatusChanged` `OutboxMessage` (`Destination = order-status-changed`) is written in the same transaction as the status change and published by the outbox publisher.

## Design

```
shared/Orders.Core/Infrastructure/
  Outbox/OutboxMessage.cs
  Outbox/DomainEventsToOutboxInterceptor.cs   # spec 01
  Outbox/DomainEventMapper.cs                 # event → (Destination, EventType, contract, CorrelationId)
  Outbox/OutboxPublisher.cs                   # BackgroundService used by both the API and the worker
  Messaging/MessagingOptions.cs               # interval, batch size, max attempts
  Messaging/MessagingServiceCollectionExtensions.cs  # AddServiceBusMessaging()
```

- Flow: `Order.Create` records `OrderCreated` → the use case calls `SaveChangesAsync` → the interceptor writes the `OutboxMessage` → single commit.
- `OutboxPublisher` every 1 s (configurable): fetches up to 20 pending messages ordered by `CreatedAt` with `SELECT … FOR UPDATE SKIP LOCKED` inside a transaction, sends them and sets `ProcessedAt`.
- The publisher lives in `Orders.Core` because both the API and the worker run it (registered by `AddServiceBusMessaging()`). Both read the **same** `OutboxMessages` table, so any process can publish any pending message — they are competing publishers coordinated by `SKIP LOCKED`. This adds redundancy: if the API is down, the worker keeps draining the outbox, and vice versa.
- `ServiceBusClient` is a **singleton** with one cached `ServiceBusSender` per queue (thread-safe, reusing the AMQP connection). The old `ServiceBusPublisher` was removed.
- Delivery guarantee: **at-least-once**. Duplicates are handled by the consumers.
- Configuration: `ConnectionStrings__ServiceBus` (emulator or a real namespace).
- `Orders.Core` depends on `Azure.Messaging.ServiceBus`, but **only** in `Infrastructure/` (enforced by the architecture test in spec 12).

## Tasks

- [x] 3.1 Map `OrderCreated` → `order-created` and `OrderStatusChanged` → `order-status-changed` in `DomainEventMapper`.
- [x] 3.2 `OutboxPublisher` in `Orders.Core` with batching, `FOR UPDATE SKIP LOCKED`, message properties (criterion 4) and retry with `Attempts`/`LastError`.
- [x] 3.3 Singleton `ServiceBusClient` and cached senders via DI; remove `Services/ServiceBusPublisher.cs`.
- [x] 3.4 Register the publisher in the API and the worker.

## Verification

- Criteria 1–2 and 7: integration tests (spec 12) checking aggregate + outbox after `SaveChanges`.
- Criteria 3–4: with Compose running, create an order and check the received properties in the worker log.
- Criterion 5: `docker compose stop servicebus`, create an order, check `Attempts > 0` in pgAdmin, `docker compose start servicebus` and watch the order complete.

## Implementation notes

- Verified in Compose: messages carry `CorrelationId`, `Subject` and `EventType`; with `servicebus` stopped, the order was created, the outbox recorded `Attempts`/`LastError`, `/health/ready` returned 503 and, once restarted, the order completed (~35 s).
- Criteria 1, 2 and 7 are covered by `PersistenceTests` and `ProcessOrderTests`.
