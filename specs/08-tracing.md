# 08 — End-to-end tracing

- **Status:** Done
- **Score:** bonus **End-to-end tracing (+2)**

## Context

There was no telemetry. With an outbox and queues, an order goes through API →
database → publisher → Service Bus → worker → status queue → API → browser; without
tracing it is hard to see where a delay or an error happens.

## Requirements

- End-to-end tracing — challenge §5.

## Acceptance criteria

1. **Given** a `POST /orders`, **then** Jaeger shows **a single trace** containing: the HTTP request, the insert SQL, the outbox publish, the worker processing (with both SQL updates) and the `OrderStatusChanged` publish.
2. Messaging spans have `messaging.system = servicebus`, `messaging.destination.name`, `messaging.message.id` and `order.id`.
3. **Given** an empty `OTEL_EXPORTER_OTLP_ENDPOINT`, **then** the API and worker run normally without trying to export.
4. API and worker logs include the `TraceId` to correlate with Jaeger.

## Design

- Packages: `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `Npgsql.OpenTelemetry`.
- `ActivitySource("Orders.Messaging")` in `Orders.Core/Observability/Telemetry.cs`.
- **Propagation through the outbox**: `DomainEventsToOutboxInterceptor` (spec 01) stores the current `Activity`'s `traceparent` in `OutboxMessage.TraceParent` — for both the API and the worker, with no extra code in the use cases. The publisher creates the `publish order-created` span as a child of that context and writes `traceparent` into `ApplicationProperties`. The worker extracts it and opens `process order-created` as a child.
- The same applies to `OrderStatusChanged` (worker → API).
- Jaeger `all-in-one` in Compose receiving OTLP (gRPC 4317), UI on `:16686`.
- Service names: `orders-api` and `orders-worker`.

## Tasks

- [x] 8.1 OpenTelemetry in the API and the worker, conditional on the OTLP endpoint.
- [x] 8.2 `TraceParent` in the outbox and propagation over both queues.
- [x] 8.3 Messaging attributes on the spans.
- [x] 8.4 Jaeger in Compose (with spec 11).
- [x] 8.5 `TraceId` in the log scope.

## Verification

- Compose running → create an order → Jaeger `http://localhost:16686` → service `orders-api` → open the trace and check the spans in criterion 1.
- Criterion 3: run the API locally without the variable and check it starts without errors.

## Implementation notes

- Verified in Jaeger: a single trace with `POST orders`, SQL (`ordersdb`), `publish order-created`, `process order-created`, `publish order-status-changed` and `process order-status-changed`, spanning `orders-api` and `orders-worker`.
- Because the outbox publisher runs in both processes (spec 03), a `publish` span may show up under either service.
- Single-line logs with `TraceId`/`SpanId` and a `Queue/MessageId/OrderId/Delivery` scope.
