# 04 — Worker idempotente

- **Status:** Concluída
- **Pontuação:** obrigatória (Mensageria & Confiabilidade 20%)

## Contexto

O worker atual não compila, injeta `DbContext` scoped num singleton (falha ao
iniciar), faz polling no banco em vez de consumir o Service Bus e não está no
Compose. Não há idempotência real: duas instâncias processariam o mesmo pedido.

## Requisitos

- Worker consome mensagens, muda para `Processando` e, após 5 s, para `Finalizado` — desafio §1.
- Consumidor idempotente — desafio §1 *Regras adicionais*.
- Sequência obrigatória de status — desafio §1.

## Critérios de aceite

1. **Dado** uma mensagem `OrderCreated` de um pedido `Pendente`, **quando** o worker a processa, **então** o pedido fica `Processando` imediatamente e `Finalizado` ~5 s depois, com 2 novas entradas no histórico; a mensagem é completada.
2. **Dado** a mesma mensagem entregue de novo após o pedido estar `Finalizado`, **então** nada muda no banco e a mensagem é completada.
3. **Dado** uma reentrega com o pedido em `Processando` (worker caiu no meio), **então** o worker retoma e finaliza, sem duplicar a entrada `Processando` no histórico.
4. **Dado** duas entregas simultâneas da mesma mensagem, **então** só uma faz cada transição (a outra recebe conflito de concorrência, recarrega e se comporta como 2 ou 3).
5. **Dado** uma mensagem cujo pedido não existe ou cujo corpo é inválido, **então** vai para a dead-letter com motivo descritivo.
6. **Dado** uma mensagem com `EventType` diferente de `OrderCreated`, **então** vai para a dead-letter.
7. **Dado** um erro transitório (ex.: banco indisponível), **então** a mensagem é abandonada e reentregue até `MaxDeliveryCount`, depois vai para a dead-letter.
8. A cada transição, o evento de domínio `OrderStatusChanged` vira mensagem de outbox na mesma transação e é publicado na fila `order-status-changed` (usado pela spec 06) — sem envio direto ao Service Bus dentro do handler.

## Design

```
shared/Orders.Core/Application/Orders/
  ProcessOrder.cs               # caso de uso idempotente: carrega o agregado e aplica TransitionTo conforme o estado

worker/Orders.Worker/
  Program.cs                    # Host + DI + ServiceBusProcessor + AddOutboxPublisher()
  OrderCreatedProcessor.cs      # BackgroundService: inicia/para o ServiceBusProcessor
  OrderCreatedHandler.cs        # adaptador: valida a mensagem, chama ProcessOrder, decide complete/dead-letter/abandon
  Heartbeat.cs                  # grava /tmp/worker-heartbeat a cada 10 s
```

O `ProcessOrder` (aplicação) não sabe nada de Service Bus: recebe o `OrderId`,
usa o agregado e salva. O `OrderCreatedHandler` (worker) só traduz a mensagem
para o caso de uso e o resultado para a ação na fila.

- `ServiceBusProcessor` com `AutoCompleteMessages = false`, `MaxConcurrentCalls` configurável (padrão 4), lock padrão (30 s > 5 s de processamento).
- **Um escopo de DI por mensagem** (`IServiceScopeFactory`) → `OrderContext` novo por mensagem.
- Idempotência = **máquina de estados + concorrência otimista**: o estado atual do pedido decide o que fazer; `xmin` impede que duas entregas façam a mesma transição. Não é necessária tabela de "mensagens processadas", porque cada transição só pode acontecer uma vez.
- Retorno do handler: `Completed`, `DeadLetter(motivo)` ou exceção (→ abandon).
- Atraso de 5 s configurável (`Worker__ProcessingDelaySeconds`) para os testes rodarem rápido.
- `OrderStatusChanged` sai pelo **outbox** (spec 03): a notificação é gravada na mesma transação da mudança de status, então não se perde se o Service Bus falhar naquele instante.
- Projeto referencia só `Orders.Core`; Dockerfile com contexto na raiz do repo.

## Tasks

- [x] 4.1 Trocar polling por `ServiceBusProcessor` na fila `order-created`.
- [x] 4.2 Caso de uso `ProcessOrder` em `Orders.Core/Application` (critérios 1–4), com tratamento de `DbUpdateConcurrencyException`.
- [x] 4.3 `OrderCreatedHandler` com escopo por mensagem, chamando `ProcessOrder`.
- [x] 4.4 Validação de `EventType`/corpo e dead-letter (critérios 5–7).
- [x] 4.5 Registrar `AddOutboxPublisher()` no worker (critério 8).
- [x] 4.6 Heartbeat para healthcheck.
- [x] 4.7 Remover referência à API; Dockerfile com contexto na raiz.

## Verificação

- Critérios 1–4 e 8: testes do `ProcessOrder` com Postgres em Testcontainers (spec 12), conferindo também o `OutboxMessage` gerado.
- Critérios 5–7: testes do handler + inspeção da dead-letter no log.
- Ponta a ponta: Compose no ar, criar pedido e acompanhar `docker compose logs -f worker`.

## Notas de implementação

- Verificado: critérios 1–4 e 8 por `ProcessOrderTests` (inclui duas entregas concorrentes: exatamente uma finaliza); 5–6 por `OrderCreatedHandlerTests`; fluxo ponta a ponta no Compose.
- Critério 7 (abandon até `MaxDeliveryCount` = 5) depende do comportamento do Service Bus e não tem teste automatizado.
