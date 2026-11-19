# 03 — Mensageria confiável: Outbox + Service Bus

- **Status:** Concluída
- **Pontuação:** obrigatória + bônus **Outbox Pattern (+3)** (Mensageria & Confiabilidade 20%)

## Contexto

O `ServiceBusPublisher` existe, mas nunca é chamado — criar pedido não publica
nada. Publicar direto no controller teria o problema clássico de *dual write*:
se o banco grava e o envio falha (ou o contrário), pedido e mensagem divergem.

Com o DDD tático (spec 01), o agregado registra **eventos de domínio** e um
interceptor do EF os converte em `OutboxMessage` no `SaveChanges`. Nenhum caso
de uso monta mensagem na mão. O mesmo mecanismo é usado pela API (`OrderCreated`)
e pelo worker (`OrderStatusChanged`).

## Requisitos

- Ao criar pedido, persistir e publicar no Azure Service Bus — desafio §1.
- `CorrelationId = OrderId` e `EventType = OrderCreated` — desafio §1 *Regras adicionais*.
- Outbox Pattern — desafio §5 (bônus).

## Critérios de aceite

1. **Dado** um `POST /orders` bem-sucedido, **então** o pedido e um `OutboxMessage` (`Type = OrderCreated`, `Destination = order-created`, `ProcessedAt = null`) foram gravados **na mesma transação**, sem que o caso de uso referencie o outbox.
2. **Dado** uma falha ao salvar, **então** nem o pedido nem a mensagem existem.
3. **Dado** um `OutboxMessage` pendente e o Service Bus disponível, **quando** o publicador roda, **então** em até ~2 s a mensagem está na fila de destino e `ProcessedAt` é preenchido.
4. A mensagem `OrderCreated` publicada tem: `MessageId = OutboxMessage.Id`, `CorrelationId = OrderId`, `Subject = "OrderCreated"`, `ApplicationProperties["EventType"] = "OrderCreated"`, `ContentType = application/json` e corpo `OrderCreatedMessage`.
5. **Dado** o Service Bus fora do ar, **quando** o publicador tenta enviar, **então** a mensagem continua pendente, `Attempts` e `LastError` são atualizados e ela é reenviada quando o Service Bus voltar; a API continua aceitando pedidos.
6. **Dado** duas instâncias do publicador, **então** a mesma mensagem não é enviada duas vezes no fluxo normal (e, se for, a idempotência do worker — spec 04 — absorve).
7. **Dado** uma transição de status feita pelo worker, **então** um `OutboxMessage` `OrderStatusChanged` (`Destination = order-status-changed`) é gravado na mesma transação da mudança de status e publicado pelo publicador do worker.

## Design

```
shared/Orders.Core/Infrastructure/
  Outbox/OutboxMessage.cs
  Outbox/DomainEventsToOutboxInterceptor.cs   # spec 01
  Outbox/DomainEventMapper.cs                 # evento → (Destination, EventType, contrato, CorrelationId)
  Outbox/OutboxPublisher.cs                   # BackgroundService reutilizado por API e worker
  Messaging/ServiceBusOptions.cs              # ConnectionString, intervalo, tamanho do lote
  Messaging/ServiceCollectionExtensions.cs    # AddServiceBusMessaging(), AddOutboxPublisher()
```

- Fluxo: `Order.Create` registra `OrderCreated` → caso de uso chama `SaveChangesAsync` → interceptor grava `OutboxMessage` → commit único.
- `OutboxPublisher` a cada 1 s (configurável): busca até 20 pendentes ordenadas por `CreatedAt` com `SELECT … FOR UPDATE SKIP LOCKED` dentro de uma transação (evita envio duplo entre instâncias), envia para `Destination` e marca `ProcessedAt`.
- O publicador fica em `Orders.Core` porque API e worker o executam (registrado por `AddServiceBusMessaging()`). Os dois leem a **mesma** tabela `OutboxMessages`, então qualquer processo pode publicar qualquer mensagem pendente — são publicadores concorrentes, coordenados pelo `SKIP LOCKED`. Isso dá redundância: se a API cair, o worker continua escoando o outbox, e vice-versa.
- `ServiceBusClient` **singleton** e um `ServiceBusSender` por fila, em cache (thread-safe, reaproveitam a conexão AMQP). O `ServiceBusPublisher` atual é removido.
- Garantia de entrega: **at-least-once**. Duplicatas são tratadas pelos consumidores.
- Configuração: `ConnectionStrings__ServiceBus` (emulador ou namespace real).
- `Orders.Core` passa a depender de `Azure.Messaging.ServiceBus`, mas **só** em `Infrastructure/` (regra verificada pelo teste de arquitetura da spec 12).

## Tasks

- [x] 3.1 Mapeamento `OrderCreated` → `order-created` e `OrderStatusChanged` → `order-status-changed` no `DomainEventMapper`.
- [x] 3.2 `OutboxPublisher` em `Orders.Core` com lote, `FOR UPDATE SKIP LOCKED`, propriedades da mensagem (critério 4), retry com `Attempts`/`LastError`.
- [x] 3.3 `ServiceBusClient` singleton e cache de senders via DI; remover `Services/ServiceBusPublisher.cs`.
- [x] 3.4 Registrar o publicador na API e no worker.

## Verificação

- Critérios 1–2 e 7: testes de integração (spec 12) que conferem agregado + outbox após o `SaveChanges`.
- Critérios 3–4: com o Compose no ar, criar pedido e ver no log do worker as propriedades recebidas.
- Critério 5: `docker compose stop servicebus`, criar pedido, conferir `Attempts > 0` no pgAdmin, `docker compose start servicebus` e ver o pedido ser finalizado.

## Notas de implementação

- Verificado no Compose: mensagens com `CorrelationId`, `Subject` e `EventType`; com o `servicebus` parado, o pedido foi criado, o outbox registrou `Attempts`/`LastError`, `/health/ready` respondeu 503 e, ao religar, o pedido foi finalizado (~35 s).
- Critérios 1, 2 e 7 cobertos por `PersistenceTests` e `ProcessOrderTests`.
