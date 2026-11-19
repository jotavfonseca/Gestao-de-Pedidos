# 08 — Tracing ponta a ponta

- **Status:** Concluída
- **Pontuação:** bônus **Tracing ponta-a-ponta (+2)**

## Contexto

Não há nenhuma telemetria. Com outbox e fila, um pedido passa por API →
banco → publicador → Service Bus → worker → fila de status → API → navegador;
sem tracing é difícil enxergar onde está a demora ou o erro.

## Requisitos

- Tracing ponta a ponta — desafio §5.

## Critérios de aceite

1. **Dado** um `POST /orders`, **então** no Jaeger existe **um único trace** contendo: requisição HTTP, comando SQL do insert, publicação do outbox, processamento no worker (com as duas atualizações SQL) e a publicação de `OrderStatusChanged`.
2. Os spans de mensageria têm atributos `messaging.system = servicebus`, `messaging.destination.name`, `messaging.message.id` e `order.id`.
3. **Dado** `OTEL_EXPORTER_OTLP_ENDPOINT` vazio, **então** API e worker funcionam normalmente, sem tentar exportar.
4. Logs de API e worker incluem `TraceId` para correlacionar com o Jaeger.

## Design

- Pacotes: `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `Npgsql.OpenTelemetry`.
- `ActivitySource("Orders.Messaging")` em `Orders.Core/Observability/Telemetry.cs`.
- **Propagação pelo outbox**: o `DomainEventsToOutboxInterceptor` (spec 01) salva o `traceparent` da `Activity` atual em `OutboxMessage.TraceParent` — vale para a API e para o worker sem código extra nos casos de uso. O publicador cria o span `publish order-created` como filho desse contexto e grava `traceparent` em `ApplicationProperties`. O worker extrai e abre `process order-created` como filho.
- O mesmo vale para `OrderStatusChanged` (worker → API).
- Jaeger `all-in-one` no Compose recebendo OTLP (gRPC 4317), UI em `:16686`.
- Nome de serviço: `orders-api` e `orders-worker`.

## Tasks

- [x] 8.1 OpenTelemetry na API e no worker, condicionado ao endpoint OTLP.
- [x] 8.2 `TraceParent` no outbox e propagação nas duas filas.
- [x] 8.3 Atributos de mensageria nos spans.
- [x] 8.4 Jaeger no Compose (junto da spec 11).
- [x] 8.5 `TraceId` no escopo de log.

## Verificação

- Compose no ar → criar pedido → Jaeger `http://localhost:16686` → serviço `orders-api` → abrir o trace e conferir os spans do critério 1.
- Critério 3: rodar a API localmente sem a variável e conferir que sobe sem erros.

## Notas de implementação

- Verificado no Jaeger: um único trace com `POST orders`, SQL (`ordersdb`), `publish order-created`, `process order-created`, `publish order-status-changed` e `process order-status-changed`, cobrindo `orders-api` e `orders-worker`.
- Como o publicador do outbox roda nos dois processos (spec 03), o span `publish` pode aparecer sob qualquer um dos serviços.
- Logs no formato de uma linha com `TraceId`/`SpanId` e escopo `Queue/MessageId/OrderId/Delivery`.
