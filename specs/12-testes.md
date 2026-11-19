# 12 — Testes automatizados

- **Status:** Concluída
- **Pontuação:** bônus **Testcontainers (+1)** e **Golden Tests (+2)**; Testes Automatizados 10%

## Contexto

Não existe nenhum teste. As regras mais críticas (sequência de status,
idempotência, outbox transacional) precisam de prova automatizada.

## Requisitos

- Testcontainers e Golden Tests — desafio §5.
- Testes automatizados — critérios de avaliação.

## Critérios de aceite

1. `dotnet test` na raiz roda todos os testes.
2. **Unitários de domínio**: transições válidas, inválidas, geração de histórico e de eventos de domínio; value object `Money` (validação, arredondamento, igualdade); validação de `cliente`/`produto` no agregado.
2a. **Arquitetura**: teste por reflexão garante que nenhum tipo em `Orders.Core.Domain` depende de EF Core, Npgsql ou Azure SDK, e que `Application` não depende de `Azure.Messaging.ServiceBus`.
2b. **Outbox**: com Testcontainers, salvar um agregado com eventos gera os `OutboxMessage` corretos (destino, `EventType`, `CorrelationId`, payload) na mesma transação.
3. **Golden tests**: o JSON de `OrderResponse` e de `OrderCreatedEvent` é comparado com arquivos versionados em `tests/Orders.Tests/Golden/`; mudança de contrato quebra o teste com diff legível; `UPDATE_GOLDEN=1 dotnet test` regenera.
4. **Worker com Testcontainers (Postgres)**: (a) fluxo feliz, (b) mensagem duplicada após finalizado, (c) reentrega em `Processando`, (d) entregas concorrentes, (e) pedido inexistente → dead-letter.
5. **API com Testcontainers**: POST cria pedido **e** outbox; 400 de validação; GET por id com histórico; 404; lista ordenada.
6. **Ferramentas do módulo IA**: contagens, somas e tempo médio corretos com dados conhecidos.
7. **Health**: `/health/live` responde 200 na API de teste.
8. Sem Docker disponível, os testes de integração são **pulados** com mensagem clara; os unitários e golden continuam rodando.
9. Frontend: `npm run lint` e `npm run build` limpos.

## Design

```
tests/Orders.Tests/
  Domain/OrderTests.cs
  Domain/MoneyTests.cs
  Architecture/LayeringTests.cs            # reflexão sobre as referências de tipos (sem pacote extra)
  Infrastructure/OutboxInterceptorTests.cs
  Golden/                       # *.json versionados
  Golden/GoldenFile.cs          # helper Assert + UPDATE_GOLDEN
  Golden/ContractGoldenTests.cs
  Infrastructure/PostgresFixture.cs        # Testcontainers.PostgreSql, migra uma vez por coleção
  Infrastructure/ApiFactory.cs             # WebApplicationFactory; Service Bus substituído por fakes
  Infrastructure/DockerRequiredFact.cs     # pula se não houver Docker
  Application/ProcessOrderTests.cs         # idempotência do caso de uso do worker
  Worker/OrderCreatedHandlerTests.cs       # validação de mensagem e dead-letter
  Api/OrdersApiTests.cs
  Ai/OrderAnalyticsToolsTests.cs
```

- xUnit, `Testcontainers.PostgreSql`, `Microsoft.AspNetCore.Mvc.Testing`.
- Golden com helper próprio (comparação de JSON normalizado), sem dependência extra.
- Datas fixas via `TimeProvider` falso para JSON determinístico.
- Atraso do worker configurado para 0 s nos testes.
- `OutboxPublisher` e consumidores de Service Bus desligados no `ApiFactory`.
- Opcional, se o tempo permitir: teste com o emulador do Service Bus em container.

## Tasks

- [x] 12.1 Projeto `tests/Orders.Tests` na solução.
- [x] 12.2 Testes de domínio (agregado, eventos, `Money`).
- [x] 12.2a Teste de arquitetura (camadas).
- [x] 12.2b Teste do interceptor de outbox.
- [x] 12.3 Golden tests + helper + arquivos.
- [x] 12.4 `PostgresFixture` e `DockerRequiredFact`.
- [x] 12.5 Testes do `ProcessOrder` (idempotência) e do `OrderCreatedHandler` (dead-letter).
- [x] 12.6 `ApiFactory` e testes da API.
- [x] 12.7 Testes das ferramentas do módulo IA.
- [x] 12.8 Lint e build do frontend verificados.

## Verificação

- `dotnet test` com Docker ligado → tudo verde.
- `dotnet test` com Docker desligado → integração pulada, resto verde.
- Alterar um campo do `OrderResponse` → golden falha; `UPDATE_GOLDEN=1` regenera.

## Notas de implementação

- 52 testes, todos passando (`dotnet test`, ~12 s com Docker).
- Pulo sem Docker via atributo `[DockerFact]` (`Infrastructure/DockerAvailability.cs`); este caminho não foi exercitado (Docker estava ligado).
- O `WebApplicationFactory` usa `OrdersController` como marcador do assembly, porque o worker também expõe uma classe `Program` aos testes.
- Golden files em `tests/Orders.Tests/Golden/files/`, gravados sem escapes Unicode para facilitar a leitura do diff.
